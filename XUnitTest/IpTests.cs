using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

public class IpTests
{
    static IpTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        IpResolver.Register();

        var addr = "39.144.10.35".IPToAddress();

        //#if DEBUG
        if (!"data/ip.gz".AsFile().Exists) Thread.Sleep(9000);
        //#endif
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

    /// <summary>构造大于3MB的测试数据库文件，跳过 Ip.Init 下载逻辑</summary>
    static String BuildBigFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "XUnitTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "test.ip");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);
        TestDbBuilder.Pad(file);
        return file;
    }

    /// <summary>清理临时目录</summary>
    static void Cleanup(String file)
    {
        var dir = Path.GetDirectoryName(file);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Fact(DisplayName = "Init 合法大文件返回True且幂等")]
    public void Init_Ok()
    {
        var file = BuildBigFile();
        var ip = new Ip { DbFile = file };
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
            Cleanup(file);
        }
    }

    [Fact(DisplayName = "GetAddress 字符串IP返回区域地址")]
    public void GetAddress_String()
    {
        var file = BuildBigFile();
        var ip = new Ip { DbFile = file };
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
            Cleanup(file);
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
        var file = BuildBigFile();
        var ip = new Ip { DbFile = file };
        try
        {
            var rs = ip.GetAddress(System.Net.IPAddress.Parse("0.0.2.100"));
            Assert.Equal("测试省C 测试运营商C", rs);
        }
        finally
        {
            // 释放 MMF 句柄，否则临时文件无法删除
            ip.Db?.Dispose();
            Cleanup(file);
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