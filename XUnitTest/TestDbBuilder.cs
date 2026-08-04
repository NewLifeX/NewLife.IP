using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace XUnitTest;

/// <summary>测试IP数据库构造器，生成纯真兼容格式的最小数据库文件</summary>
/// <remarks>直接使用 Stream 字节写入，避免 BinaryWriter.Write(Byte) 内部缓冲与手动 Position 混用导致字节错位</remarks>
internal static class TestDbBuilder
{
    /// <summary>测试记录定义</summary>
    public record DbRecord(UInt32 Start, UInt32 End, String Area, String Addr, Boolean Redirect);

    /// <summary>默认测试记录：3条，覆盖 0.0.0.0 ~ 0.0.2.255，末条为重定向结构</summary>
    public static DbRecord[] DefaultRecords { get; } =
    [
        new(0x00000000u, 0x000000FFu, "测试省A", "测试运营商A", false),
        new(0x00000100u, 0x000001FFu, "测试省B", "测试运营商B", false),
        new(0x00000200u, 0x000002FFu, "测试省C", "测试运营商C", true),
    ];

    static Encoding _enc;

    /// <summary>GB2312 编码（调用方需先注册 CodePagesEncodingProvider）</summary>
    static Encoding Enc => _enc ??= Encoding.GetEncoding("GB2312");

    /// <summary>构造测试数据库文件（纯真兼容：头部 + 索引区 + 数据区）</summary>
    /// <param name="file">目标文件路径</param>
    /// <param name="records">记录列表</param>
    public static void Build(String file, IList<DbRecord> records)
    {
        using var ms = new MemoryStream();

        var indexStart = 8u;
        var indexBytes = (UInt32)(records.Count * 7);

        // 先计算数据区偏移
        var offsets = new UInt32[records.Count];
        var pos = indexStart + indexBytes;
        for (var i = 0; i < records.Count; i++)
        {
            offsets[i] = pos;
            pos += BlockSize(records[i]);
        }

        // 头部：索引区起止偏移
        WriteUInt32(ms, indexStart);
        WriteUInt32(ms, indexStart + indexBytes - 1);

        // 索引区：Start(4B) + Offset(3B)
        for (var i = 0; i < records.Count; i++)
        {
            WriteUInt32(ms, records[i].Start);
            WriteOffset(ms, offsets[i]);
        }

        // 数据区
        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            if (r.Redirect)
            {
                // 重定向结构：End + 标记(tag=2, 区域指向别处) + 地址；区域独立成块
                WriteUInt32(ms, r.End);
                var areaPos = offsets[i] + 4 + 4 + ByteCount(r.Addr) + 1;
                WriteUInt32(ms, 0x02u | (areaPos << 8));
                WriteString(ms, r.Addr);
                WriteString(ms, r.Area);
            }
            else
            {
                // 普通结构：End + 区域 + 地址
                WriteUInt32(ms, r.End);
                WriteString(ms, r.Area);
                WriteString(ms, r.Addr);
            }
        }

        File.WriteAllBytes(file, ms.ToArray());
    }

    /// <summary>数据块字节数</summary>
    static UInt32 BlockSize(DbRecord r)
    {
        var sz = 4u + ByteCount(r.Area) + 1 + ByteCount(r.Addr) + 1;
        // 重定向结构多 4 字节标记
        if (r.Redirect) sz += 4;
        return sz;
    }

    /// <summary>GB2312 字节数</summary>
    static UInt32 ByteCount(String s) => (UInt32)Enc.GetByteCount(s);

    /// <summary>写入小端 UInt32</summary>
    static void WriteUInt32(Stream ms, UInt32 value)
    {
        ms.WriteByte((Byte)value);
        ms.WriteByte((Byte)(value >> 8));
        ms.WriteByte((Byte)(value >> 16));
        ms.WriteByte((Byte)(value >> 24));
    }

    /// <summary>写入 3 字节偏移</summary>
    static void WriteOffset(Stream ms, UInt32 offset)
    {
        ms.WriteByte((Byte)offset);
        ms.WriteByte((Byte)(offset >> 8));
        ms.WriteByte((Byte)(offset >> 16));
    }

    /// <summary>写入 GB2312 字符串 + 0 终止符</summary>
    static void WriteString(Stream ms, String s)
    {
        var buf = Enc.GetBytes(s);
        ms.Write(buf, 0, buf.Length);
        ms.WriteByte(0);
    }

    /// <summary>填充文件到指定大小，模拟真实大文件以跳过 Ip.Init 的下载逻辑</summary>
    /// <param name="file">目标文件</param>
    /// <param name="minBytes">最小字节数</param>
    public static void Pad(String file, Int32 minBytes = 3 * 1024 * 1024)
    {
        using var fs = File.OpenWrite(file);
        if (fs.Length < minBytes) fs.SetLength(minBytes + 100);
    }

    /// <summary>将文件压缩为gz格式</summary>
    /// <param name="file">源文件</param>
    /// <param name="gzFile">gz目标文件</param>
    public static void CompressGz(String file, String gzFile)
    {
        using var fs = File.OpenRead(file);
        using var gz = File.Create(gzFile);
        using var gzStream = new GZipStream(gz, CompressionMode.Compress);
        fs.CopyTo(gzStream);
    }
}
