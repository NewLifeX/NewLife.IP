using System.Net;
using NewLife.Net;

namespace NewLife.IP;

/// <summary>IP地址解析器</summary>
public class IpResolver : IIPResolver
{
    private Ip _ip = new();
    private OnlineIp _online;

    /// <summary>是否使用在线解析。默认false（离线），设为true后所有查询走在线API</summary>
    public static Boolean Online { get; set; }

    /// <summary>在线解析器</summary>
    OnlineIp OnlineDb => _online ??= new OnlineIp();

    /// <summary>获取物理地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public String GetAddress(IPAddress ip)
    {
        try
        {
            return Online ? OnlineDb.GetAddress(ip) : _ip.GetAddress(ip);
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
            return Online ? OnlineDb.GetAddress(ip) : _ip.GetAddress(ip);
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