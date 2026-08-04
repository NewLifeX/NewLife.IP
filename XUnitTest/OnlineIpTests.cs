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
}
