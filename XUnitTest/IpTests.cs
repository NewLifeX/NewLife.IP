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
        Assert.Equal("", ss[1]);
    }

    [Fact]
    public void Test多线程()
    {
        Parallel.For(0, 100, i =>
        {
            var addr = "116.136.7.43".IPToAddress();
            var ss = addr.Split(' ');
            Assert.Equal("中国–内蒙古–赤峰", ss[0]);
            Assert.Equal("", ss[1]);
        });
    }
}