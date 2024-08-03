using System.Net;
using NewLife;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

public class IpHelperTests
{
    [Fact]
    public void ToUInt32Test()
    {
        var str = "47.100.59.126";
        var rs = IPAddress.TryParse(str, out var addr);
        Assert.Equal(str, addr.ToString());

        var ids = str.SplitAsInt(".");
        var buf = addr.GetAddressBytes();
        for (var i = 0; i < ids.Length; i++)
        {
            Assert.Equal(ids[i], buf[i]);
        }
        Assert.Equal("2F643B7E", buf.ToHex());
        var ori = (ids[0] << 24) + (ids[1] << 16) + (ids[2] << 8) + ids[3];
        Assert.Equal(0x2F643B7E, ori);

        var n = addr.ToUInt32();
        Assert.Equal(0x2F643B7Eu, n);
    }

    [Fact]
    public void ToAddressTest()
    {
        var str = "47.100.59.126";

        var n = 0x2F643B7Eu;
        var addr = n.ToAddress();
        Assert.Equal(str, addr.ToString());
    }

    [Fact]
    public void ToUInt32IP()
    {
        var str = "47.100.59.126";

        var n = 0x2F643B7Eu;
        var addr = str.ToUInt32IP();
        Assert.Equal(n, addr);
    }

    [Fact]
    public void ToStringIP()
    {
        var str = "47.100.59.126";

        var addr = str.ToUInt32IP();
        Assert.Equal("047.100.059.126", addr.ToStringIP());
    }
}