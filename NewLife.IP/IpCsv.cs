using System.IO;
using System.Text;
using NewLife.IO;
using NewLife.Log;

namespace NewLife.IP;

/// <summary>IP数据库转换器：从DB-IP CSV数据生成纯真兼容数据库文件，扩展数据源支持</summary>
/// <remarks>
/// DB-IP 提供免费 CSV 数据（https://db-ip.com，CC BY 4.0），列序为：
/// start_ip,end_ip,country_code,country_name,state_prov,district,city,...
/// 本转换器读取 CSV 并生成纯真兼容格式文件，随后可复用 <see cref="IpDatabase"/> 的 MMF + 二分检索查询。
/// DB-IP 无运营商字段，地址（运营商）部分为空。
/// </remarks>
public static class IpCsv
{
    /// <summary>转换DB-IP CSV到纯真兼容数据库文件</summary>
    /// <param name="csvFile">DB-IP CSV文件路径</param>
    /// <param name="dbFile">输出数据库文件路径</param>
    /// <returns>写入的记录数</returns>
    /// <remarks>CSV 需包含表头行；自动跳过表头、非法 IP 行与空区域行。</remarks>
    public static Int32 Convert(String csvFile, String dbFile)
    {
        if (csvFile.IsNullOrEmpty()) throw new ArgumentNullException(nameof(csvFile));
        if (dbFile.IsNullOrEmpty()) throw new ArgumentNullException(nameof(dbFile));
        if (!File.Exists(csvFile)) throw new FileNotFoundException("找不到CSV文件", csvFile);

        // 读取CSV并解析记录
        var records = new List<DbRecord>();
        using (var csv = new CsvFile(csvFile, false))
        {
            String[] line;
            while ((line = csv.ReadLine()) != null)
            {
                // 至少需要 start/end/country/state/district/city 六列
                if (line.Length < 7) continue;

                var startText = line[0].Trim();
                var endText = line[1].Trim();
                // 跳过表头与非法IP行
                if (!IsIpv4(startText) || !IsIpv4(endText)) continue;

                var start = startText.ToUInt32IP();
                var end = endText.ToUInt32IP();
                if (end < start) continue;

                // 区域：国家–省–区–市，跳过空字段
                var area = BuildArea(line[3], line[4], line[5], line[6]);
                if (area.IsNullOrEmpty()) continue;

                records.Add(new DbRecord(start, end, area));
            }
        }

        if (records.Count == 0) throw new InvalidOperationException("CSV中没有有效记录");

        // 按起始IP排序，保证二分检索正确
        records.Sort((x, y) => x.Start.CompareTo(y.Start));

        WriteDbFile(dbFile, records);

        XTrace.WriteLine("IP数据库转换完成：{0:n0} 条记录 → {1}", records.Count, dbFile);

        return records.Count;
    }

    /// <summary>拼接区域字符串：国家–省–区–市（跳过空字段）</summary>
    /// <param name="parts">区域字段</param>
    /// <returns>拼接后的区域字符串</returns>
    static String BuildArea(params String[] parts)
    {
        var ss = new List<String>();
        foreach (var part in parts)
        {
            var s = part.Trim();
            if (!s.IsNullOrEmpty()) ss.Add(s);
        }
        return String.Join("–", ss);
    }

    /// <summary>判断是否为合法IPv4字符串</summary>
    /// <param name="s">IP字符串</param>
    /// <returns>是否合法IPv4</returns>
    static Boolean IsIpv4(String s)
    {
        if (s.IsNullOrEmpty()) return false;

        var ss = s.Split('.');
        if (ss.Length != 4) return false;

        foreach (var p in ss)
        {
            if (!UInt32.TryParse(p, out var v) || v > 255) return false;
        }
        return true;
    }

    /// <summary>生成纯真兼容数据库文件（头部 + 索引区 + 数据区）</summary>
    /// <param name="dbFile">输出文件路径</param>
    /// <param name="records">排序后的记录</param>
    static void WriteDbFile(String dbFile, List<DbRecord> records)
    {
        var n = records.Count;
        var indexStart = 8u;
        var indexBytes = (UInt32)(n * 7);

        // 计算数据块偏移
        var enc = Encoding.GetEncoding("GB2312");
        var offsets = new UInt32[n];
        var pos = indexStart + indexBytes;
        for (var i = 0; i < n; i++)
        {
            offsets[i] = pos;
            pos += 4u + (UInt32)enc.GetByteCount(records[i].Area) + 1u + 1u;
        }

        using var fs = File.Create(dbFile);

        // 头部：索引区起止偏移
        WriteUInt32(fs, indexStart);
        WriteUInt32(fs, indexStart + indexBytes - 1);

        // 索引区：Start(4B) + Offset(3B)
        for (var i = 0; i < n; i++)
        {
            WriteUInt32(fs, records[i].Start);
            WriteOffset(fs, offsets[i]);
        }

        // 数据区：End(4B) + 区域 + 0 + 空地址 + 0
        for (var i = 0; i < n; i++)
        {
            WriteUInt32(fs, records[i].End);
            var area = enc.GetBytes(records[i].Area);
            fs.Write(area, 0, area.Length);
            fs.WriteByte(0);
            fs.WriteByte(0);
        }
    }

    /// <summary>写入小端UInt32</summary>
    /// <param name="stream">数据流</param>
    /// <param name="value">值</param>
    static void WriteUInt32(Stream stream, UInt32 value)
    {
        stream.WriteByte((Byte)value);
        stream.WriteByte((Byte)(value >> 8));
        stream.WriteByte((Byte)(value >> 16));
        stream.WriteByte((Byte)(value >> 24));
    }

    /// <summary>写入3字节偏移</summary>
    /// <param name="stream">数据流</param>
    /// <param name="offset">偏移</param>
    static void WriteOffset(Stream stream, UInt32 offset)
    {
        stream.WriteByte((Byte)offset);
        stream.WriteByte((Byte)(offset >> 8));
        stream.WriteByte((Byte)(offset >> 16));
    }

    /// <summary>转换记录</summary>
    struct DbRecord
    {
        public UInt32 Start;
        public UInt32 End;
        public String Area;

        public DbRecord(UInt32 start, UInt32 end, String area)
        {
            Start = start;
            End = end;
            Area = area;
        }
    }
}
