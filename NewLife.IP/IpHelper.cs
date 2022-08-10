using Microsoft.Extensions.DependencyInjection;
using NewLife.Net;

namespace NewLife.IP;

/// <summary>IP地址助手</summary>
public static class IpHelper
{
    /// <summary>注册IP地址解析器</summary>
    public static void Register()
    {
        if (NetHelper.IpResolver is not IpResolver)
            NetHelper.IpResolver = new IpResolver();
    }

    /// <summary>注册IP地址解析器</summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IIPResolver AddIpResolver(this IServiceCollection services)
    {
        if (NetHelper.IpResolver is not IpResolver)
            NetHelper.IpResolver = new IpResolver();

        var resolver = NetHelper.IpResolver;

        services.AddSingleton(resolver);

        return resolver;
    }
}