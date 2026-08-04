using System;
using System.IO;
using System.Text;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

/// <summary>IP数据库转换器测试：验证 DB-IP CSV 转换与生成文件可查询</summary>
public class IpCsvTests
{
    static IpCsvTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>写入CSV测试文件</summary>
    static String BuildCsv(String dir, String content)
    {
        var file = Path.Combine(dir, "dbip.csv");
        File.WriteAllText(file, content, Encoding.UTF8);
        return file;
    }

    /// <summary>创建临时目录</summary>
    static String CreateDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XUnitTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>清理临时目录</summary>
    static void Cleanup(String dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Fact(DisplayName = "Convert 转换DB-IP CSV生成可查询数据库")]
    public void Convert_Ok()
    {
        var dir = CreateDir();
        try
        {
            var csv = BuildCsv(dir, "start_ip,end_ip,country_code,country_name,state_prov,district,city\n" +
                "1.0.0.0,1.0.0.255,AU,Australia,Queensland,,Brisbane\n" +
                "2.0.0.0,2.0.0.255,CN,China,Guangdong,,Shenzhen\n" +
                "3.0.0.0,3.0.0.255,US,United States,California,,Los Angeles\n");
            var dbFile = Path.Combine(dir, "ip.db");

            var count = IpCsv.Convert(csv, dbFile);
            Assert.Equal(3, count);

            using var db = new IpDatabase();
            db.SetFile(dbFile);
            Assert.Equal(3u, db.Count);

            var (area1, addr1) = db.GetAddress(0x01000000u); // 1.0.0.0
            Assert.Equal("Australia–Queensland–Brisbane", area1);
            Assert.Equal("", addr1);

            var (area2, addr2) = db.GetAddress(0x02000064u); // 2.0.0.100
            Assert.Equal("China–Guangdong–Shenzhen", area2);
            Assert.Equal("", addr2);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact(DisplayName = "Convert 处理引号与逗号字段")]
    public void Convert_Quoted()
    {
        var dir = CreateDir();
        try
        {
            var csv = BuildCsv(dir, "\"start_ip\",\"end_ip\",\"country_code\",\"country_name\",\"state_prov\",\"district\",\"city\"\n" +
                "\"4.0.0.0\",\"4.0.0.255\",\"GB\",\"United Kingdom\",\"England, London\",\"\",\"London\"\n");
            var dbFile = Path.Combine(dir, "ip.db");

            var count = IpCsv.Convert(csv, dbFile);
            Assert.Equal(1, count);

            using var db = new IpDatabase();
            db.SetFile(dbFile);
            var (area, _) = db.GetAddress(0x04000000u);
            Assert.Equal("United Kingdom–England, London–London", area);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact(DisplayName = "Convert 跳过表头与无效行")]
    public void Convert_Filter()
    {
        var dir = CreateDir();
        try
        {
            var csv = BuildCsv(dir, "start_ip,end_ip,country_code,country_name,state_prov,district,city\n" +
                "5.0.0.0,5.0.0.255,JP,Japan,Tokyo,,Tokyo\n" +
                "invalid,5.0.1.255,XX,Bad,,,\n" + // 非法IP行
                "5.0.2.0,5.0.2.255,,,,\n"); // 空区域行
            var dbFile = Path.Combine(dir, "ip.db");

            var count = IpCsv.Convert(csv, dbFile);
            Assert.Equal(1, count);

            using var db = new IpDatabase();
            db.SetFile(dbFile);
            Assert.Equal(1u, db.Count);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact(DisplayName = "Convert 参数校验与文件不存在")]
    public void Convert_Args()
    {
        Assert.Throws<ArgumentNullException>(() => IpCsv.Convert(null, "x"));
        Assert.Throws<ArgumentNullException>(() => IpCsv.Convert("x", null));
        Assert.Throws<FileNotFoundException>(() => IpCsv.Convert("not_exist.csv", "x"));
    }
}
