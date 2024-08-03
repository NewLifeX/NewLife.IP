using System.Net;

namespace NewLife.IP;

/// <summary>IP地址助手</summary>
public static class IpHelper
{
    /// <summary>转为大端整数IP</summary>
    /// <param name="addr"></param>
    /// <returns></returns>
    public static UInt32 ToUInt32(this IPAddress addr)
    {
        var buf = addr.GetAddressBytes();
        return (UInt32)(buf[0] << 24 | buf[1] << 16 | buf[2] << 8 | buf[3]);
    }

    /// <summary>大端整数IP转地址</summary>
    /// <param name="addr"></param>
    /// <returns></returns>
    public static IPAddress ToAddress(this UInt32 addr)
    {
        var buf = new Byte[4];
        buf[0] = (Byte)(addr >> 24);
        buf[1] = (Byte)((addr >> 16) & 0xFF);
        buf[2] = (Byte)((addr >> 8) & 0xFF);
        buf[3] = (Byte)(addr & 0xFF);

        return new IPAddress(buf);
    }

    /// <summary>IP字符串转为整数IP</summary>
    /// <param name="ipValue"></param>
    /// <returns></returns>
    public static UInt32 ToUInt32IP(this String ipValue)
    {
        var ss = ipValue.Split('.');
        //var buf = stackalloc Byte[4];
        var val = 0u;
        //var ptr = (Byte*)&val;
        for (var i = 0; i < 4; i++)
        {
            if (i < ss.Length && UInt32.TryParse(ss[i], out var n))
            {
                //buf[3 - i] = (Byte)n;
                // 感谢啊富弟（QQ125662872）指出错误，右边需要乘以8，这里为了避开乘法，采用位移实现
                val |= n << ((3 - i) << 3);
                //ptr[3 - i] = n;
            }
        }
        //return BitConverter.ToUInt32(buf, 0);
        return val;
    }

    /// <summary>整数IP转IP字符串</summary>
    public static String ToStringIP(this UInt32 ip) => $"{(ip >> 24) & 0xFF:000}.{(ip >> 16) & 0xFF:000}.{(ip >> 8) & 0xFF:000}.{ip & 0xFF:000}";
}