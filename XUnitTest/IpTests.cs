using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

public class IpTests : IClassFixture<BigFileFixture>
{
    readonly BigFileFixture _fixture;

    public IpTests(BigFileFixture fixture) => _fixture = fixture;

    static IpTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        IpResolver.Register();

        // 数据文件由 XUnitTest.csproj 复制到输出目录，缺失时立即失败，
        // 避免 Ip.Init 走联网下载白等 5 秒（历史实现此处固定休眠 9 秒）
        var dataFile = Path.Combine(AppContext.BaseDirectory, "Data", "ip.gz");
        if (!File.Exists(dataFile)) throw new FileNotFoundException($"测试数据缺失：{dataFile}", dataFile);

        // 预热真实数据库，后续用例共享同一实例
        _ = "39.144.10.35".IPToAddress();
    }

    [Fact]
    public void Test1()
    {
        var addr = "39.144.10.35".IPToAddress();
        var ss = addr.Split(' ');
        Assert.Equal("中国–广东", ss[0]);

        addr = "116.234.91.199".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–上海–上海", ss[0]);

        addr = "61.160.219.25".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–江苏–常州–武进区", ss[0]);

        addr = "123.14.85.208".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–河南–郑州", ss[0]);

        addr = "113.220.60.29".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–湖南–邵阳", ss[0]);

        addr = "124.239.170.77".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–河北–衡水", ss[0]);

        addr = "112.74.79.65".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–广东–深圳", ss[0]);

        addr = "218.87.90.59".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–江西–九江–永修县", ss[0]);

        addr = "39.144.8.87".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–广东–深圳", ss[0]);

        addr = "111.55.141.170".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("中国–山西", ss[0]);
    }

    [Fact]
    public void Test自治区()
    {
        var addr = "116.136.7.43".IPToAddress();
        var ss = addr.Split(' ');
        Assert.Equal("中国–内蒙古–赤峰", ss[0]);
        Assert.Equal("联通", ss[1]);
    }

    [Fact]
    public void Test多线程()
    {
        Parallel.For(0, 100, i =>
        {
            var addr = "116.136.7.43".IPToAddress();
            var ss = addr.Split(' ');
            Assert.Equal("中国–内蒙古–赤峰", ss[0]);
            Assert.Equal("联通", ss[1]);
        });
    }

    #region 构造数据测试（不依赖真实数据库）

    [Fact(DisplayName = "Init 合法大文件返回True且幂等")]
    public void Init_Ok()
    {
        var ip = new Ip { DbFile = _fixture.File };
        try
        {
            Assert.True(ip.Init());
            Assert.True(ip.Init());
            Assert.NotNull(ip.Db);
        }
        finally
        {
            // 释放 MMF 句柄，否则临时文件无法删除
            ip.Db?.Dispose();
        }
    }

    [Fact(DisplayName = "GetAddress 字符串IP返回区域地址")]
    public void GetAddress_String()
    {
        var ip = new Ip { DbFile = _fixture.File };
        try
        {
            var (area, addr) = ip.GetAddress("0.0.1.100");
            Assert.Equal("测试省B", area);
            Assert.Equal("测试运营商B", addr);
        }
        finally
        {
            // 释放 MMF 句柄，否则临时文件无法删除
            ip.Db?.Dispose();
        }
    }

    [Fact(DisplayName = "GetAddress 非法IP字符串按0.0.0.0处理")]
    public void GetAddress_Invalid()
    {
        var ip = new Ip { DbFile = _fixture.File };
        try
        {
            var (area, addr) = ip.GetAddress("not-an-ip");
            Assert.Equal("测试省A", area);
            Assert.Equal("测试运营商A", addr);
        }
        finally
        {
            ip.Db?.Dispose();
        }
    }

    [Fact(DisplayName = "GetAddress 空字符串返回空")]
    public void GetAddress_Empty()
    {
        var ip = new Ip();
        var (area, addr) = ip.GetAddress("");
        Assert.Equal("", area);
        Assert.Equal("", addr);
    }

    [Fact(DisplayName = "GetAddress IPAddress对象返回拼接字符串")]
    public void GetAddress_IPAddress()
    {
        var ip = new Ip { DbFile = _fixture.File };
        try
        {
            var rs = ip.GetAddress(System.Net.IPAddress.Parse("0.0.2.100"));
            Assert.Equal("测试省C 测试运营商C", rs);
        }
        finally
        {
            // 释放 MMF 句柄，否则临时文件无法删除
            ip.Db?.Dispose();
        }
    }

    [Fact(DisplayName = "GetAddress 空IPAddress返回空串")]
    public void GetAddress_IPAddress_Null()
    {
        var ip = new Ip();
        Assert.Equal("", ip.GetAddress((System.Net.IPAddress)null));
    }

    #endregion
}

/// <summary>大于3MB的测试数据库夹具，整个测试类共享一份，避免每个用例重复构造</summary>
public sealed class BigFileFixture : IDisposable
{
    /// <summary>数据库文件路径</summary>
    public String File { get; }

    public BigFileFixture()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XUnitTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File = Path.Combine(dir, "test.ip");
        TestDbBuilder.Build(File, TestDbBuilder.DefaultRecords);
        TestDbBuilder.Pad(File);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(File);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}