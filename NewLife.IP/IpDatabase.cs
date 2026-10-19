using System.IO.MemoryMappedFiles;
using System.Text;
using NewLife.Log;

namespace NewLife.IP;

/// <summary>IP数据库</summary>
public class IpDatabase : IDisposable
{
    #region 属性
    /// <summary>起始位置</summary>
    public UInt32 Start { get; private set; }

    /// <summary>结束位置</summary>
    public UInt32 End { get; private set; }

    /// <summary>记录数</summary>
    public UInt32 Count { get; private set; }

    /// <summary>日志对象。默认 <see cref="XTrace.Log"/>，设为 <see cref="Logger.Null"/> 可关闭日志</summary>
    public ILog Log { get => _log ??= XTrace.Log; set => _log = value; }

    /// <summary>日志前缀。前缀在消息翻译之后拼接，默认“[IpDatabase] ”</summary>
    public String LogPrefix { get; set; } = "[IpDatabase] ";

    private ILog _log;
    MemoryMappedFile _mmf;
    MemoryMappedViewAccessor _view;
    String _tempFile;
    #endregion

    #region 构造
    /// <summary>析构</summary>
    ~IpDatabase() { OnDispose(false); }

    /// <summary>销毁</summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        OnDispose(true);
    }

    void OnDispose(Boolean disposing)
    {
        if (disposing)
        {
            _view.TryDispose();
            _mmf.TryDispose();

            if (!_tempFile.IsNullOrEmpty() && File.Exists(_tempFile)) File.Delete(_tempFile);

            GC.SuppressFinalize(this);
        }
    }
    #endregion

    #region 数据源
    /// <summary>设置文件</summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public IpDatabase SetFile(String file)
    {
        // 仅支持Gzip压缩，可用7z软件先压缩为gz格式
        if (file.EndsWithIgnoreCase(".gz"))
        {
            // 解压缩到临时文件
            var file2 = Path.GetTempFileName();

            using var fs2 = File.Create(file2);
            using var fs = File.OpenRead(file);
            fs.DecompressGZip(fs2);

            // 重新打开，切换到文件流
            file = file2;
            _tempFile = file2;
        }

        // 启用MMF
        _mmf = MemoryMappedFile.CreateFromFile(file, FileMode.Open, null, 0, MemoryMappedFileAccess.ReadWrite);

        var view = _view ??= _mmf.CreateViewAccessor();
        Start = view.ReadUInt32(0);
        End = view.ReadUInt32(4);
        Count = (End - Start) / 7u + 1;

        Log?.InfoPrefixed(LogPrefix, "IP record count: {0:n0}", Count);

        return this;
    }
    #endregion

    #region 方法
    /// <summary>获取IP的物理地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public (String area, String addr) GetAddress(UInt32 ip)
    {
        // 频繁销毁视图会导致性能下降
        //using var view = _mmf.CreateViewAccessor();
        var view = _view ??= _mmf.CreateViewAccessor();

        // 二分法搜索，找到IP所在区间
        // 索引区间之外的IP必须返回空；旧实现会在区间外反复收缩到同一索引，导致死循环
        var low = 0;
        var high = (Int32)Count - 1;
        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var inf = ReadIndexInfo(view, (UInt32)mid);

            if (ip < inf.Start)
                high = mid - 1;
            else if (ip > inf.End)
                low = mid + 1;
            else
                return ReadAddressInfo(view, inf.Offset);
        }

        return (String.Empty, String.Empty);
    }

    /// <summary>获取指定索引处的信息</summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public (IndexInfo, String area, String addr) GetIndex(UInt32 idx)
    {
        var view = _view ??= _mmf.CreateViewAccessor();

        var info = ReadIndexInfo(view, idx);
        var (area, addr) = ReadAddressInfo(view, info.Offset);
        return (info, area, addr);
    }

    (String area, String addr) ReadAddressInfo(UnmanagedMemoryAccessor view, UInt32 offset)
    {
        String area;
        String addr;

        var p = offset + 4;
        // MMF 视图容量跨平台不同（Windows 按页对齐、Linux 精确等于文件大小），
        // 对截断或尾部记录显式边界保护，避免 Linux 越界异常
        if (p + 4 > view.Capacity) return (String.Empty, String.Empty);

        var v = view.ReadUInt32(p);
        var tag = v & 0xFF;
        if (tag == 1)
        {
            // 整体指向其它位置
            p = v >> 8;
            v = view.ReadUInt32(p);
            tag = v & 0xFF;
            if (tag == 2)
            {
                // 只有区域，地址在别的地方
                offset = v >> 8;
                addr = ReadAddress(view, p + 4);
                area = ReadString(view, ref offset);
            }
            else
            {
                // 地址与区域连续
                area = ReadString(view, ref p);
                addr = ReadAddress(view, p);
            }
        }
        else
        {
            if (tag == 2)
            {
                // 只有区域，地址在别的地方
                offset = v >> 8;
                addr = ReadAddress(view, p + 4);
                area = ReadString(view, ref offset);
            }
            else
            {
                // 地址与区域连续
                area = ReadString(view, ref p);
                addr = ReadAddress(view, p);
            }
        }
        return (area, addr);
    }

    String ReadAddress(UnmanagedMemoryAccessor view, UInt32 p)
    {
        // 普通记录末尾的空地址仅 1 个终止符 0 字节且可能位于文件尾，剩余不足 4 字节时按空地址处理；
        // Windows 视图容量页对齐可越界读零，Linux 精确容量会抛异常，必须显式保护
        if (p + 4 > view.Capacity) return String.Empty;

        var v = view.ReadUInt32(p);
        var tag = v & 0xFF;
        if (tag == 1 || tag == 2)
            p = v >> 8;

        return ReadString(view, ref p);
    }

    [ThreadStatic]
    private static Byte[] _buf;
    private static Encoding _encoding;
    String ReadString(UnmanagedMemoryAccessor view, ref UInt32 p)
    {
        // 64字节缓冲可覆盖绝大多数地名，但多语言长地名（GB18030 下更占字节）必须按需扩容，
        // 否则会截断文本并把读取位置推进到字符串中间，后续地址也会跟着错位
        var buf = _buf ??= new Byte[64];
        var len = 0;
        var terminated = false;
        while (true)
        {
            var offset = p + (UInt32)len;
            if (offset >= view.Capacity) break;

            if (len == buf.Length)
            {
                if (buf.Length >= 4096)
                {
                    // 到达上限时单独确认紧跟的终止符，避免恰好 4KB 的文本被误判为未终止
                    if (offset < view.Capacity && view.ReadByte(offset) == 0) terminated = true;
                    break;
                }

                Array.Resize(ref buf, buf.Length * 2);
                _buf = buf;
            }

            var available = (Int32)Math.Min(buf.Length - len, view.Capacity - offset);
            var count = view.ReadArray(offset, buf, len, available);
            if (count <= 0) break;

            var end = Array.IndexOf(buf, (Byte)0, len, count);
            if (end >= 0)
            {
                len = end;
                terminated = true;
                break;
            }

            len += count;
        }

        if (len <= 0)
        {
            // 空字符串也要跳过 0 终止符，否则后续地址会从终止符位置开始读取
            if (terminated) p += 1;
            return String.Empty;
        }

        // 跳过0终止符，确保后续地址字符串从内容开始读取
        p += (UInt32)len + (terminated ? 1u : 0u);

        _encoding ??= IpEncoding.Db;

        var str = _encoding.GetString(buf, 0, len).Trim().Trim('\0').Trim();
        if (str == "CZ88.NET") return String.Empty;

        return str;
    }

    IndexInfo ReadIndexInfo(UnmanagedMemoryAccessor view, UInt32 index)
    {
        var p = Start + 7u * index;
        var inf = new IndexInfo
        {
            Start = view.ReadUInt32(p),
            Offset = ReadOffset(view, p + 4)
        };
        inf.End = view.ReadUInt32(inf.Offset);

        return inf;
    }

    static unsafe UInt32 ReadOffset(UnmanagedMemoryAccessor view, UInt32 p)
    {
        var buf = _buf ??= new Byte[64];
        view.ReadArray<Byte>(p, buf, 0, 3);
        return buf[0] | (UInt32)buf[1] << 8 | (UInt32)buf[2] << 16;
    }
    #endregion
}