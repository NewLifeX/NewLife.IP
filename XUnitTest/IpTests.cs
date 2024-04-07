using System.IO;
using System.Text;
using System.Threading;
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
        Assert.Equal("广东省", ss[0]);

        addr = "116.234.91.199".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("上海市", ss[0]);

        addr = "61.160.219.25".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("江苏省常州市武进区", ss[0]);

        addr = "123.14.85.208".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("河南省郑州市", ss[0]);

        addr = "113.220.60.29".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("湖南省邵阳市", ss[0]);

        addr = "124.239.170.77".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("河北省衡水市", ss[0]);

        addr = "112.74.79.65".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("广东省深圳市", ss[0]);

        addr = "218.87.90.59".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("江西省九江市永修县", ss[0]);

        addr = "39.144.8.87".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("广东省深圳市", ss[0]);

        addr = "111.55.141.170".IPToAddress();
        ss = addr.Split(' ');
        Assert.Equal("山西省", ss[0]);
    }

    [Fact]
    public void Test自治区()
    {
        var addr = "116.136.7.43".IPToAddress();
        var ss = addr.Split(' ');
        Assert.Equal("内蒙古赤峰市", ss[0]);
        Assert.Equal("联通", ss[1]);
    }
}