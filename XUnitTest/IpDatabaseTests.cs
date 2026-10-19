using System;
using System.IO;
using System.Text;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

/// <summary>IP数据库测试：用构造的最小数据库文件验证加载、索引遍历与二分检索</summary>
public class IpDatabaseTests : IClassFixture<IpDatabaseFixture>
{
    readonly IpDatabaseFixture _fixture;

    public IpDatabaseTests(IpDatabaseFixture fixture) => _fixture = fixture;

    static IpDatabaseTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>构造自定义记录集的临时数据库文件</summary>
    static String BuildFile(params TestDbBuilder.DbRecord[] records)
    {
        var file = Path.GetTempFileName();
        TestDbBuilder.Build(file, records);
        return file;
    }

    [Fact(DisplayName = "SetFile 打开数据库，头部解析正确")]
    public void SetFile_Head()
    {
        using var db = new IpDatabase();
        db.SetFile(_fixture.File);

        Assert.Equal(8u, db.Start);
        Assert.Equal(8u + 3 * 7 - 1, db.End);
        Assert.Equal(3u, db.Count);
    }

    [Fact(DisplayName = "SetFile 打开gz压缩库，自动解压到临时文件")]
    public void SetFile_Gz()
    {
        var gz = _fixture.File + ".gz";
        TestDbBuilder.CompressGz(_fixture.File, gz);

        using var db = new IpDatabase();
        db.SetFile(gz);

        Assert.Equal(8u, db.Start);
        Assert.Equal(3u, db.Count);
    }

    [Fact(DisplayName = "GetIndex 遍历索引，返回对应记录")]
    public void GetIndex_All()
    {
        using var db = new IpDatabase();
        db.SetFile(_fixture.File);

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
        db.SetFile(_fixture.File);

        var (area, addr) = db.GetAddress(0x00000000u);
        Assert.Equal("测试省A", area);
        Assert.Equal("测试运营商A", addr);
    }

    [Fact(DisplayName = "GetAddress 命中区间内IP")]
    public void GetAddress_Mid()
    {
        using var db = new IpDatabase();
        db.SetFile(_fixture.File);

        var (area, addr) = db.GetAddress(0x00000164u); // 0.0.1.100
        Assert.Equal("测试省B", area);
        Assert.Equal("测试运营商B", addr);
    }

    [Fact(DisplayName = "GetAddress 命中重定向记录")]
    public void GetAddress_Redirect()
    {
        using var db = new IpDatabase();
        db.SetFile(_fixture.File);

        var (area, addr) = db.GetAddress(0x00000264u); // 0.0.2.100
        Assert.Equal("测试省C", area);
        Assert.Equal("测试运营商C", addr);
    }

    [Fact(DisplayName = "GetAddress 索引区间之外返回空且不死循环")]
    public void GetAddress_OutOfRange()
    {
        using var db = new IpDatabase();
        db.SetFile(_fixture.File);

        // 大于末条记录结束IP
        var (area1, addr1) = db.GetAddress(0xFFFFFFFFu);
        Assert.Equal("", area1);
        Assert.Equal("", addr1);

        // 小于首条记录起始IP
        var file = BuildFile(new TestDbBuilder.DbRecord(0x01000000u, 0x010000FFu, "测试省A", "测试运营商A", false));
        try
        {
            using var db2 = new IpDatabase();
            db2.SetFile(file);

            var (area2, addr2) = db2.GetAddress(0x00FFFFFFu);
            Assert.Equal("", area2);
            Assert.Equal("", addr2);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact(DisplayName = "GetAddress 索引间隙返回空")]
    public void GetAddress_Gap()
    {
        var file = BuildFile(
            new TestDbBuilder.DbRecord(0x00000000u, 0x000000FFu, "测试省A", "测试运营商A", false),
            new TestDbBuilder.DbRecord(0x00000200u, 0x000002FFu, "测试省C", "测试运营商C", false));
        try
        {
            using var db = new IpDatabase();
            db.SetFile(file);

            var (area, addr) = db.GetAddress(0x00000180u);
            Assert.Equal("", area);
            Assert.Equal("", addr);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact(DisplayName = "GetAddress 多语言长文本不截断")]
    public void GetAddress_MultilingualLongText()
    {
        var area = "中国–" + new String('北', 40) + "–São Paulo–القاهرة–😀";
        var addr = "运营商–São Paulo–القاهرة";
        var file = BuildFile(new TestDbBuilder.DbRecord(0x03000000u, 0x030000FFu, area, addr, false));
        try
        {
            using var db = new IpDatabase();
            db.SetFile(file);

            var (a, b) = db.GetAddress(0x03000010u);
            Assert.Equal(area, a);
            Assert.Equal(addr, b);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact(DisplayName = "GetAddress 空区域时仍能读取后续地址")]
    public void GetAddress_EmptyArea()
    {
        var file = BuildFile(new TestDbBuilder.DbRecord(0x04000000u, 0x040000FFu, "", "运营商", false));
        try
        {
            using var db = new IpDatabase();
            db.SetFile(file);

            var (area, addr) = db.GetAddress(0x04000010u);
            Assert.Equal("", area);
            Assert.Equal("运营商", addr);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact(DisplayName = "Dispose 重复销毁不抛异常")]
    public void Dispose_Twice()
    {
        var db = new IpDatabase();
        db.SetFile(_fixture.File);
        db.Dispose();
        db.Dispose();
    }
}

/// <summary>默认测试数据库夹具，整个测试类共享一份，避免每个用例重复落盘</summary>
public sealed class IpDatabaseFixture : IDisposable
{
    /// <summary>数据库文件路径</summary>
    public String File { get; }

    public IpDatabaseFixture()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XUnitTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File = Path.Combine(dir, "test.ip");
        TestDbBuilder.Build(File, TestDbBuilder.DefaultRecords);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(File);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}
