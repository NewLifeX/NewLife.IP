using System.Net;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Net;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.IP;

/// <summary>在线IP解析器：调用第三方在线API查询IP归属地，支持缓存与容错</summary>
/// <remarks>
/// 默认使用 ip-api.com 免费接口（无 Key），返回格式：
/// <c>{"status":"success","country":"United States","regionName":"Virginia","city":"Ashburn","isp":"Google LLC","query":"8.8.8.8"}</c>
/// 通过 <see cref="Server"/> 可配置为同格式的其他在线 API；网络失败 / 限流 / 解析失败返回空，不抛异常影响业务。
/// </remarks>
public class OnlineIp : IIPResolver
{
    #region 属性
    /// <summary>在线API地址。默认 ip-api.com，{0} 为IP占位符</summary>
    public String Server { get; set; } = "http://ip-api.com/json/{0}";

    /// <summary>超时时间。默认5000毫秒</summary>
    public Int32 Timeout { get; set; } = 5_000;

    /// <summary>查询缓存。默认null不缓存，可注入 <see cref="MemoryCache.Instance"/> 等 ICache 实现</summary>
    public ICache Cache { get; set; }

    /// <summary>缓存过期秒数。默认24小时，0表示不缓存</summary>
    public Int32 Expire { get; set; } = 24 * 3600;

    private WebClientX _client;
    #endregion

    #region 方法
    /// <summary>获取IP地址所映射的物理地址</summary>
    /// <param name="ip">IP字符串</param>
    /// <returns>区域与地址，失败返回空</returns>
    public (String area, String addr) GetAddress(String ip)
    {
        if (ip.IsNullOrEmpty()) return ("", "");

        var cache = Cache;
        var key = "OnlineIp:" + ip;
        var useCache = cache != null && Expire > 0;

        // 命中缓存直接返回
        if (useCache)
        {
            var val = cache.Get<String>(key);
            if (!val.IsNullOrEmpty())
            {
                var p = val.IndexOf('\t');
                if (p >= 0) return (val.Substring(0, p), val.Substring(p + 1));
            }
        }

        var (area, addr) = Request(ip);

        // 查询成功才写缓存，避免缓存失败结果
        if (useCache && !area.IsNullOrEmpty())
            cache.Set(key, area + "\t" + addr, Expire);

        return (area, addr);
    }

    /// <summary>获取IP地址所映射的物理地址</summary>
    /// <param name="ip">IP地址</param>
    /// <returns>拼接字符串，失败返回空</returns>
    public String GetAddress(IPAddress ip)
    {
        if (ip == null) return "";

        var (area, addr) = GetAddress(ip.ToString());
        return area + " " + addr;
    }

    /// <summary>请求在线API并解析</summary>
    /// <param name="ip">IP字符串</param>
    /// <returns>区域与地址</returns>
    (String area, String addr) Request(String ip)
    {
        try
        {
            var client = _client ??= new WebClientX { Timeout = Timeout, Log = XTrace.Log };
            var url = String.Format(Server, ip);
            var json = client.GetHtml(url);

            return Parse(json);
        }
        catch (Exception ex)
        {
            // 在线查询失败不抛异常，返回空
            XTrace.WriteException(ex);
            return ("", "");
        }
    }

    /// <summary>解析在线API响应（ip-api.com JSON格式）</summary>
    /// <param name="json">JSON响应</param>
    /// <returns>区域与地址</returns>
    public static (String area, String addr) Parse(String json)
    {
        if (json.IsNullOrEmpty()) return ("", "");

        try
        {
            var dic = JsonParser.Decode(json);
            if (dic == null) return ("", "");

            // ip-api.com 成功响应 status=success
            if (dic.TryGetValue("status", out var status) && status?.ToString() != "success")
                return ("", "");

            var country = GetField(dic, "country");
            var region = GetField(dic, "regionName");
            var city = GetField(dic, "city");
            var isp = GetField(dic, "isp");

            var area = Join(country, region, city);
            return (area, isp ?? "");
        }
        catch
        {
            return ("", "");
        }
    }

    /// <summary>获取字典字段字符串</summary>
    /// <param name="dic">字典</param>
    /// <param name="key">字段名</param>
    /// <returns>字段值，不存在返回null</returns>
    static String GetField(IDictionary<String, Object> dic, String key)
    {
        if (dic.TryGetValue(key, out var value) && value != null) return value.ToString();
        return null;
    }

    /// <summary>拼接区域字符串，过滤空字段</summary>
    /// <param name="parts">区域字段</param>
    /// <returns>拼接结果</returns>
    static String Join(params String[] parts)
    {
        var ss = new List<String>();
        foreach (var part in parts)
        {
            if (!part.IsNullOrEmpty()) ss.Add(part);
        }
        return String.Join("–", ss);
    }
    #endregion
}
