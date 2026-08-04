using System;
using System.Net;
using NewLife;
using NewLife.IP;
using NewLife.Net;
using Xunit;

namespace XUnitTest;

/// <summary>IP解析器测试：验证注册幂等性与异常兜底</summary>
public class IpResolverTests
{
    [Fact(DisplayName = "Register 注册解析器到 NetHelper")]
    public void Register_Ok()
    {
        IpResolver.Register();
        Assert.IsType<IpResolver>(NetHelper.IpResolver);
    }

    [Fact(DisplayName = "Register 重复注册保持同一实例")]
    public void Register_Same()
    {
        IpResolver.Register();
        var first = NetHelper.IpResolver;
        IpResolver.Register();
        Assert.Same(first, NetHelper.IpResolver);
    }

    [Fact(DisplayName = "GetAddress 空IPAddress返回空串")]
    public void GetAddress_Null()
    {
        var resolver = new IpResolver();
        Assert.Equal("", resolver.GetAddress((IPAddress)null));
    }

    [Fact(DisplayName = "GetAddress 空字符串返回空元组")]
    public void GetAddress_Empty()
    {
        var resolver = new IpResolver();
        var (area, addr) = resolver.GetAddress("");
        Assert.Equal("", area);
        Assert.Equal("", addr);
    }

    [Fact(DisplayName = "Online 属性默认离线")]
    public void Online_Default()
    {
        Assert.False(IpResolver.Online);
    }

    [Fact(DisplayName = "在线模式空输入返回空")]
    public void GetAddress_Online_Empty()
    {
        IpResolver.Online = true;
        try
        {
            var resolver = new IpResolver();
            Assert.Equal("", resolver.GetAddress((IPAddress)null));

            var (area, addr) = resolver.GetAddress("");
            Assert.Equal("", area);
            Assert.Equal("", addr);
        }
        finally
        {
            IpResolver.Online = false;
        }
    }
}
