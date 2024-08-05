using System.Net;
using NewLife.Net;

namespace NewLife.IP;

/// <summary>IP地址解析器</summary>
public class IpResolver : IIPResolver
{
    private Ip _ip = new();

    /// <summary>获取物理地址</summary>
    /// <param name="addr"></param>
    /// <returns></returns>
    public String GetAddress(IPAddress addr)
    {
        try
        {
            return _ip.GetAddress(addr);
        }
        catch
        {
            return String.Empty;
        }
    }

    /// <summary>注册IP地址解析器</summary>
    public static void Register()
    {
        if (NetHelper.IpResolver is not IpResolver)
            NetHelper.IpResolver = new IpResolver();
    }
}