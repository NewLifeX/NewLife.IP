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

        XTrace.WriteLine("IP记录数：{0:n0}", Count);

        return this;
    }
    #endregion

    #region 方法
    /// <summary>获取IP的物理地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public (String area, String addr) GetAddress(UInt32 ip)
    {
        var idxSet = 0u;
        var idxEnd = Count - 1u;
        // 频繁销毁视图会导致性能下降
        //using var view = _mmf.CreateViewAccessor();
        var view = _view ??= _mmf.CreateViewAccessor();

        // 二分法搜索，找到IP所在区间
        IndexInfo set;
        while (true)
        {
            set = ReadIndexInfo(view, idxSet);
            if (ip >= set.Start && ip <= set.End) break;

            var end = ReadIndexInfo(view, idxEnd);
            if (ip >= end.Start && ip <= end.End) return ReadAddressInfo(view, end.Offset);

            var mid = ReadIndexInfo(view, (idxEnd + idxSet) / 2u);
            if (ip >= mid.Start && ip <= mid.End) return ReadAddressInfo(view, mid.Offset);

            if (ip < mid.Start)
                idxEnd = (idxEnd + idxSet) / 2u;
            else
                idxSet = (idxEnd + idxSet) / 2u;
        }
        return ReadAddressInfo(view, set.Offset);
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
        var buf = _buf ??= new Byte[64];
        var count = view.ReadArray(p, buf, 0, buf.Length);

        var k = 0u;
        while (k < count && buf[k] != 0) k++;
        if (k == 0) return String.Empty;

        p += k;

        _encoding ??= Encoding.GetEncoding("GB2312");

        var str = _encoding.GetString(buf, 0, (Int32)k).Trim().Trim('\0').Trim();
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