using System.IO.MemoryMappedFiles;
using System.Text;
using NewLife.Log;

namespace NewLife.IP;

class Zip : IDisposable
{
    #region 属性
    UInt32 _Start;
    UInt32 _End;
    UInt32 _Count;

    MemoryMappedFile _mmf;
    String _tempFile;

    /// <summary>数据流</summary>
    public Stream Stream { get; set; }
    #endregion

    #region 构造
    /// <summary>析构</summary>
    ~Zip() { OnDispose(false); }

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
            Stream?.Dispose();
            _mmf.TryDispose();

            if (!_tempFile.IsNullOrEmpty() && File.Exists(_tempFile)) File.Delete(_tempFile);

            GC.SuppressFinalize(this);
        }
    }
    #endregion

    #region 数据源
    public Zip SetFile(String file)
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
        Stream = _mmf.CreateViewStream();

        using var view = _mmf.CreateViewAccessor();
        _Start = view.ReadUInt32(0);
        _End = view.ReadUInt32(4);
        _Count = (_End - _Start) / 7u + 1u;

        XTrace.WriteLine("IP记录数：{0:n0}", _Count);

        return this;
    }
    #endregion

    #region 方法
    public String GetAddress(UInt32 ip)
    {
        var idxSet = 0u;
        var idxEnd = _Count - 1u;
        using var view = _mmf.CreateViewAccessor();

        // 二分法搜索
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

    String ReadAddressInfo(UnmanagedMemoryAccessor view, UInt32 offset)
    {
        String addr;
        String area;

        var p = offset + 4;
        var tag = view.ReadByte(p);
        if (tag == 1)
        {
            p = view.ReadUInt32(p + 1) & 0x00FF_FFFF;
            tag = view.ReadByte(p);
            if (tag == 2)
            {
                offset = view.ReadUInt32(p + 1) & 0x00FF_FFFF;
                area = ReadArea(view, p + 4);
                addr = ReadString(view, ref offset);
            }
            else
            {
                addr = ReadString(view, ref p);
                area = ReadArea(view, p);
            }
        }
        else
        {
            if (tag == 2)
            {
                offset = view.ReadUInt32(p + 1) & 0x00FF_FFFF;
                area = ReadArea(view, p + 4);
                addr = ReadString(view, ref offset);
            }
            else
            {
                addr = ReadString(view, ref p);
                area = ReadArea(view, p);
            }
        }
        return (addr + " " + area).Trim();
    }

    UInt32 GetOffset()
    {
        var ms = Stream;
        if (ms == null) return 0;

        return BitConverter.ToUInt32(new Byte[] {
            (Byte)ms.ReadByte(),
            (Byte)ms.ReadByte(),
            (Byte)ms.ReadByte(),
            0 },
            0);
    }

    String ReadArea(UnmanagedMemoryAccessor view, UInt32 p)
    {
        var tag = view.ReadByte(p);
        if (tag == 1 || tag == 2)
            p = view.ReadUInt32(p + 1) & 0x00FF_FFFF;

        return ReadString(view, ref p);
    }

    private static Encoding _encoding;
    String ReadString(UnmanagedMemoryAccessor view, ref UInt32 p)
    {
        var buf = new Byte[256];
        var count = view.ReadArray(p, buf, 0, buf.Length);

        var k = 0u;
        while (k < count && buf[k] != 0) k++;
        p += k;

        _encoding ??= Encoding.GetEncoding("GB2312");

        var str = _encoding.GetString(buf, 0, (Int32)k).Trim().Trim('\0').Trim();
        if (str == "CZ88.NET") return String.Empty;

        return str;
    }

    Byte GetTag() => (Byte)(Stream?.ReadByte() ?? 0);

    IndexInfo ReadIndexInfo(UnmanagedMemoryAccessor view, UInt32 index)
    {
        var p = _Start + 7u * index;
        var inf = new IndexInfo
        {
            Start = view.ReadUInt32(p),
            Offset = view.ReadUInt32(p + 4) & 0x00FF_FFFF
        };
        inf.End = view.ReadUInt32(inf.Offset);

        return inf;
    }

    UInt32 GetUInt32()
    {
        var ms = Stream;
        if (ms == null) return 0;

        var array = new Byte[4];
        ms.Read(array, 0, 4);
        return BitConverter.ToUInt32(array, 0);
    }
    #endregion

    /// <summary>索引结构</summary>
    struct IndexInfo
    {
        public UInt32 Start;
        public UInt32 End;
        public UInt32 Offset;
    }
}