using System.Net;
using NewLife.Net;

namespace NewLife.IP;

/// <summary>IP地址解析器</summary>
public class IpResolver : IIPResolver
{
    private Ip _ip = new();

    /// <summary>获取物理地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public String GetAddress(IPAddress ip)
    {
        try
        {
            return _ip.GetAddress(ip);
        }
        catch
        {
            return String.Empty;
        }
    }

    /// <summary>获取指定IP的地址集合</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public (String area, String addr) GetAddress(String ip)
    {
        try
        {
            return _ip.GetAddress(ip);
        }
        catch
        {
            return (String.Empty, String.Empty);
        }
    }

    /// <summary>注册IP地址解析器</summary>
    public static void Register()
    {
        if (NetHelper.IpResolver is not IpResolver)
            NetHelper.IpResolver = new IpResolver();
    }
}