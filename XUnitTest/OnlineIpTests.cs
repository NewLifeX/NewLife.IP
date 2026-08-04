using System;
using NewLife.Caching;
using NewLife.IP;
using Xunit;

namespace XUnitTest;

/// <summary>在线IP解析器测试：验证JSON解析、空输入与缓存命中（不依赖真实网络）</summary>
public class OnlineIpTests
{
    [Fact(DisplayName = "Parse 解析ip-api成功响应")]
    public void Parse_Ok()
    {
        var json = "{\"status\":\"success\",\"country\":\"United States\",\"regionName\":\"Virginia\",\"city\":\"Ashburn\",\"isp\":\"Google LLC\",\"query\":\"8.8.8.8\"}";

        var (area, addr) = OnlineIp.Parse(json);

        Assert.Equal("United States–Virginia–Ashburn", area);
        Assert.Equal("Google LLC", addr);
    }

    [Fact(DisplayName = "Parse 部分字段缺失仍解析")]
    public void Parse_Partial()
    {
        var json = "{\"status\":\"success\",\"country\":\"China\",\"isp\":\"China Telecom\"}";

        var (area, addr) = OnlineIp.Parse(json);

        Assert.Equal("China", area);
        Assert.Equal("China Telecom", addr);
    }

    [Fact(DisplayName = "Parse 异常响应返回空")]
    public void Parse_Fail()
    {
        var json = "{\"status\":\"fail\",\"message\":\"invalid query\"}";

        var (area, addr) = OnlineIp.Parse(json);

        Assert.Equal("", area);
        Assert.Equal("", addr);
    }

    [Fact(DisplayName = "Parse 无效JSON返回空")]
    public void Parse_Invalid()
    {
        var (area, addr) = OnlineIp.Parse("not json");

        Assert.Equal("", area);
        Assert.Equal("", addr);
    }

    [Fact(DisplayName = "GetAddress 空IP返回空")]
    public void GetAddress_Empty()
    {
        var ip = new OnlineIp();

        var (area, addr) = ip.GetAddress("");
        Assert.Equal("", area);
        Assert.Equal("", addr);

        Assert.Equal("", ip.GetAddress((System.Net.IPAddress)null));
    }

    [Fact(DisplayName = "GetAddress 命中缓存不请求网络")]
    public void GetAddress_Cache()
    {
        var cache = new MemoryCache();
        var ip = new OnlineIp { Cache = cache, Expire = 3600 };

        // 预置缓存值（格式：区域\t地址）
        cache.Set("OnlineIp:8.8.8.8", "测试省\t测试运营商", 3600);

        var (area, addr) = ip.GetAddress("8.8.8.8");

        Assert.Equal("测试省", area);
        Assert.Equal("测试运营商", addr);
    }

    [Fact(DisplayName = "Parse 腾讯位置服务响应")]
    public void Parse_Tencent()
    {
        var json = "{\"status\":0,\"message\":\"query ok\",\"result\":{\"ip\":\"116.234.91.199\",\"location\":{\"lat\":31.23,\"lng\":121.47},\"ad_info\":{\"nation\":\"中国\",\"province\":\"上海市\",\"city\":\"上海市\",\"district\":\"杨浦区\",\"adcode\":310110},\"isp\":\"电信\"}}";

        var (area, addr) = OnlineIp.Parse(json);

        Assert.Equal("中国–上海市–上海市–杨浦区", area);
        Assert.Equal("电信", addr);
    }

    [Fact(DisplayName = "Parse 高德响应")]
    public void Parse_Amap()
    {
        var json = "{\"status\":\"1\",\"info\":\"OK\",\"infocode\":\"10000\",\"province\":\"广东省\",\"city\":\"深圳市\",\"adcode\":\"440300\",\"rectangle\":\"113.79,22.47;114.63,22.86\"}";

        var (area, addr) = OnlineIp.Parse(json);

        Assert.Equal("广东省–深圳市", area);
        Assert.Equal("", addr);
    }

    [Fact(DisplayName = "Parse 百度响应")]
    public void Parse_Baidu()
    {
        var json = "{\"address\":\"CN|北京|北京|None|CHINANET|0|0\",\"content\":{\"address_detail\":{\"province\":\"北京市\",\"city\":\"北京市\",\"district\":\"\",\"adcode\":\"110000\"},\"point\":{\"x\":\"116.39\",\"y\":\"39.92\"}},\"status\":0}";

        var (area, addr) = OnlineIp.Parse(json);

        Assert.Equal("北京市–北京市", area);
        Assert.Equal("CN|北京|北京|None|CHINANET|0|0", addr);
    }

    [Fact(DisplayName = "BuildUrl 默认ip-api无Key")]
    public void BuildUrl_Default()
    {
        var ip = new OnlineIp();

        Assert.Equal("http://ip-api.com/json/8.8.8.8", ip.BuildUrl("8.8.8.8"));
    }

    [Fact(DisplayName = "BuildUrl 带Key的国内厂商地址")]
    public void BuildUrl_Key()
    {
        var ip = new OnlineIp
        {
            Server = "https://apis.map.qq.com/ws/location/v1/ip?ip={0}&key={1}",
            Key = "mykey",
        };

        Assert.Equal("https://apis.map.qq.com/ws/location/v1/ip?ip=8.8.8.8&key=mykey", ip.BuildUrl("8.8.8.8"));
    }
}
