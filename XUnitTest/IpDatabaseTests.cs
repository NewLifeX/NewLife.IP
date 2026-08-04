using System;
using System.IO;
using System.Text;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

/// <summary>IP数据库测试：用构造的最小数据库文件验证加载、索引遍历与二分检索</summary>
public class IpDatabaseTests
{
    static IpDatabaseTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>构造测试数据库文件</summary>
    static String BuildFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XUnitTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "test.ip");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);
        return file;
    }

    [Fact(DisplayName = "SetFile 打开数据库，头部解析正确")]
    public void SetFile_Head()
    {
        using var db = new IpDatabase();
        db.SetFile(BuildFile());

        Assert.Equal(8u, db.Start);
        Assert.Equal(8u + 3 * 7 - 1, db.End);
        Assert.Equal(3u, db.Count);
    }

    [Fact(DisplayName = "SetFile 打开gz压缩库，自动解压到临时文件")]
    public void SetFile_Gz()
    {
        var file = BuildFile();
        var gz = file + ".gz";
        TestDbBuilder.CompressGz(file, gz);

        using var db = new IpDatabase();
        db.SetFile(gz);

        Assert.Equal(8u, db.Start);
        Assert.Equal(3u, db.Count);
    }

    [Fact(DisplayName = "GetIndex 遍历索引，返回对应记录")]
    public void GetIndex_All()
    {
        using var db = new IpDatabase();
        db.SetFile(BuildFile());

        var (info0, area0, addr0) = db.GetIndex(0);
        Assert.Equal(0x00000000u, info0.Start);
        Assert.Equal(0x000000FFu, info0.End);
        Assert.Equal("测试省A", area0);
        Assert.Equal("测试运营商A", addr0);

        var (info1, area1, addr1) = db.GetIndex(1);
        Assert.Equal(0x00000100u, info1.Start);
        Assert.Equal(0x000001FFu, info1.End);
        Assert.Equal("测试省B", area1);
        Assert.Equal("测试运营商B", addr1);

        // 重定向记录
        var (info2, area2, addr2) = db.GetIndex(2);
        Assert.Equal(0x00000200u, info2.Start);
        Assert.Equal(0x000002FFu, info2.End);
        Assert.Equal("测试省C", area2);
        Assert.Equal("测试运营商C", addr2);
    }

    [Fact(DisplayName = "GetAddress 命中区间起始IP")]
    public void GetAddress_Start()
    {
        using var db = new IpDatabase();
        db.SetFile(BuildFile());

        var (area, addr) = db.GetAddress(0x00000000u);
        Assert.Equal("测试省A", area);
        Assert.Equal("测试运营商A", addr);
    }

    [Fact(DisplayName = "GetAddress 命中区间内IP")]
    public void GetAddress_Mid()
    {
        using var db = new IpDatabase();
        db.SetFile(BuildFile());

        var (area, addr) = db.GetAddress(0x00000164u); // 0.0.1.100
        Assert.Equal("测试省B", area);
        Assert.Equal("测试运营商B", addr);
    }

    [Fact(DisplayName = "GetAddress 命中重定向记录")]
    public void GetAddress_Redirect()
    {
        using var db = new IpDatabase();
        db.SetFile(BuildFile());

        var (area, addr) = db.GetAddress(0x00000264u); // 0.0.2.100
        Assert.Equal("测试省C", area);
        Assert.Equal("测试运营商C", addr);
    }

    [Fact(DisplayName = "Dispose 重复销毁不抛异常")]
    public void Dispose_Twice()
    {
        var db = new IpDatabase();
        db.SetFile(BuildFile());
        db.Dispose();
        db.Dispose();
    }
}
