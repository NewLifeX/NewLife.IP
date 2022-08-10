using System;
using System.Collections.Generic;
using System.Text;

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
}