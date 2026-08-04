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
    /// <summary>在线API地址。默认 ip-api.com，{0} 为IP占位符，{1} 为Key占位符</summary>
    public String Server { get; set; } = "http://ip-api.com/json/{0}";

    /// <summary>API Key。腾讯/高德/百度等国内厂商需要，为空则不携带</summary>
    public String Key { get; set; }

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

    /// <summary>构造在线API请求地址（替换 {0}=IP、{1}=Key 占位符）</summary>
    /// <param name="ip">IP字符串</param>
    /// <returns>请求地址</returns>
    public String BuildUrl(String ip)
    {
        var url = Server;
        if (url.Contains("{0}")) url = url.Replace("{0}", ip);
        if (url.Contains("{1}")) url = url.Replace("{1}", Key ?? "");
        return url;
    }

    /// <summary>请求在线API并解析</summary>
    /// <param name="ip">IP字符串</param>
    /// <returns>区域与地址</returns>
    (String area, String addr) Request(String ip)
    {
        try
        {
            var client = _client ??= new WebClientX { Timeout = Timeout, Log = XTrace.Log };
            var json = client.GetHtml(BuildUrl(ip));

            return Parse(json);
        }
        catch (Exception ex)
        {
            // 在线查询失败不抛异常，返回空
            XTrace.WriteException(ex);
            return ("", "");
        }
    }

    /// <summary>解析在线API响应（支持 ip-api.com / 腾讯 / 高德 / 百度 格式自动识别）</summary>
    /// <param name="json">JSON响应</param>
    /// <returns>区域与地址</returns>
    public static (String area, String addr) Parse(String json)
    {
        if (json.IsNullOrEmpty()) return ("", "");

        try
        {
            var dic = JsonParser.Decode(json);
            if (dic == null) return ("", "");

            // 腾讯位置服务：result.ad_info.nation/province/city/district + result.isp
            if (dic.TryGetValue("result", out var result) && result is IDictionary<String, Object> resultDic)
            {
                var ad = GetDic(resultDic, "ad_info");
                var nation = GetField(ad, "nation");
                var province = GetField(ad, "province");
                var city = GetField(ad, "city");
                var district = GetField(ad, "district");
                var isp = GetField(resultDic, "isp");

                var area = Join(nation, province, city, district);
                return (area, isp ?? "");
            }

            // 百度地图：content.address_detail.province/city/district + 顶层 address
            if (dic.TryGetValue("content", out var content) && content is IDictionary<String, Object> contentDic)
            {
                var detail = GetDic(contentDic, "address_detail");
                var province = GetField(detail, "province");
                var city = GetField(detail, "city");
                var district = GetField(detail, "district");
                var addr = GetField(dic, "address");

                var area = Join(province, city, district);
                return (area, addr ?? "");
            }

            // 高德：顶层 province/city（status=1）
            if (dic.TryGetValue("status", out var status) && status?.ToString() == "1")
            {
                var province = GetField(dic, "province");
                var city = GetField(dic, "city");
                var district = GetField(dic, "district");

                var area = Join(province, city, district);
                return (area, "");
            }

            // ip-api.com：顶层 status=success + country/regionName/city/isp
            if (dic.TryGetValue("status", out var st) && st?.ToString() != "success")
                return ("", "");

            var country = GetField(dic, "country");
            var region = GetField(dic, "regionName");
            var city2 = GetField(dic, "city");
            var isp2 = GetField(dic, "isp");

            var area2 = Join(country, region, city2);
            return (area2, isp2 ?? "");
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
        if (dic != null && dic.TryGetValue(key, out var value) && value != null) return value.ToString();
        return null;
    }

    /// <summary>获取嵌套字典</summary>
    /// <param name="dic">字典</param>
    /// <param name="key">字段名</param>
    /// <returns>嵌套字典，不存在返回null</returns>
    static IDictionary<String, Object> GetDic(IDictionary<String, Object> dic, String key)
    {
        if (dic != null && dic.TryGetValue(key, out var value) && value is IDictionary<String, Object> sub)
            return sub;
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
