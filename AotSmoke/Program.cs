// NewLife.IP NativeAOT compatibility smoke test.
// Runs a fixed set of scenarios against the library and reports PASS/FAIL per scenario.
// Same binary is run under JIT (dotnet run) and NativeAOT (publish) to compare.
//
// 说明：AOT 能力验收冒烟件，由 .github/workflows/aot.yml 的 aot 矩阵自动执行（非 0 退出码即失败）。
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NewLife;
using NewLife.Caching;
using NewLife.IP;
using NewLife.Log;
using NewLife.Net;
using XUnitTest;

try { Console.OutputEncoding = Encoding.UTF8; } catch { }
try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }

Console.WriteLine("=== NewLife.IP AOT smoke ===");
Console.WriteLine($"framework={RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"IsDynamicCodeSupported={RuntimeFeature.IsDynamicCodeSupported}");

var results = new List<(String Name, Boolean Ok, String Error)>();
void Run(String name, Action action)
{
    try
    {
        action();
        results.Add((name, true, ""));
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        results.Add((name, false, $"{ex.GetType().Name}: {ex.Message}"));
        Console.WriteLine($"[FAIL] {name} -> {ex.GetType().Name}: {ex.Message}");
    }
}

var temp = Path.Combine(Path.GetTempPath(), "ip-aot-smoke-" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(temp);
XTrace.Log = new TextFileLog { LogPath = temp };
Console.WriteLine($"[diag] temp={temp}");

try
{
    // ============ CONV IP 地址转换 ============

    Run("IpHelper_StringRoundtrip", () =>
    {
        if ("1.2.3.4".ToUInt32IP() != 0x01020304u) throw new Exception("ToUInt32IP");
        if (0x01020304u.ToStringIP() != "001.002.003.004") throw new Exception($"ToStringIP={0x01020304u.ToStringIP()}");
        if ("255.255.255.255".ToUInt32IP() != 0xFFFFFFFFu) throw new Exception("ToUInt32IP 边界");
    });

    Run("IpHelper_AddressRoundtrip", () =>
    {
        if (IPAddress.Parse("8.8.4.4").ToUInt32() != 0x08080404u) throw new Exception("ToUInt32");
        if (0x08080404u.ToAddress().ToString() != "8.8.4.4") throw new Exception("ToAddress");
    });

    // ============ CORE 核心解析引擎（MMF + 二分检索 + GB2312） ============

    Run("IpDatabase_ParseNormal", () =>
    {
        var file = Path.Combine(temp, "normal.ip");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);

        using var db = new IpDatabase().SetFile(file);
        if (db.Count != 3) throw new Exception($"Count={db.Count}");

        var (area, addr) = db.GetAddress("0.0.0.100".ToUInt32IP());
        if (area != "测试省A" || addr != "测试运营商A") throw new Exception($"({area},{addr})");
    });

    Run("IpDatabase_ParseRedirect", () =>
    {
        var file = Path.Combine(temp, "redirect.ip");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);

        using var db = new IpDatabase().SetFile(file);
        var (area, addr) = db.GetAddress("0.0.2.100".ToUInt32IP());
        if (area != "测试省C" || addr != "测试运营商C") throw new Exception($"({area},{addr})");
    });

    Run("IpDatabase_GetIndexAll", () =>
    {
        var file = Path.Combine(temp, "index.ip");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);

        using var db = new IpDatabase().SetFile(file);
        for (var i = 0u; i < db.Count; i++)
        {
            var (info, area, addr) = db.GetIndex(i);
            if (area.IsNullOrEmpty() || info.Start == 0 && i > 0) throw new Exception($"idx={i} area={area}");
        }
    });

    Run("IpDatabase_GzFile", () =>
    {
        var file = Path.Combine(temp, "gz.ip");
        var gzFile = Path.Combine(temp, "gz.ip.gz");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);
        TestDbBuilder.CompressGz(file, gzFile);

        using var db = new IpDatabase().SetFile(gzFile);
        var (area, addr) = db.GetAddress("0.0.1.100".ToUInt32IP());
        if (area != "测试省B" || addr != "测试运营商B") throw new Exception($"({area},{addr})");
    });

    // ============ API 高层查询封装 ============

    Run("Ip_FullChain", () =>
    {
        var file = Path.Combine(temp, "full.ip");
        TestDbBuilder.Build(file, TestDbBuilder.DefaultRecords);
        TestDbBuilder.Pad(file);

        var ip = new Ip { DbFile = file };
        try
        {
            if (!ip.Init()) throw new Exception("Init=false");

            var (area, addr) = ip.GetAddress("0.0.1.100");
            if (area != "测试省B" || addr != "测试运营商B") throw new Exception($"({area},{addr})");

            var rs = ip.GetAddress(IPAddress.Parse("0.0.2.100"));
            if (rs != "测试省C 测试运营商C") throw new Exception($"rs={rs}");
        }
        finally
        {
            ip.Db?.Dispose();
        }
    });

    Run("Ip_EmptyInput", () =>
    {
        var ip = new Ip();
        var (area, addr) = ip.GetAddress("");
        if (area != "" || addr != "") throw new Exception($"({area},{addr})");
        if (ip.GetAddress((IPAddress)null) != "") throw new Exception("null IPAddress");
    });

    // ============ ONL 在线 IP 对接（JSON 解析，不依赖网络） ============

    Run("OnlineIp_ParseIpApi", () =>
    {
        var json = "{\"status\":\"success\",\"country\":\"United States\",\"regionName\":\"Virginia\",\"city\":\"Ashburn\",\"isp\":\"Google LLC\",\"query\":\"8.8.8.8\"}";
        var (area, addr) = OnlineIp.Parse(json);
        if (area != "United States–Virginia–Ashburn" || addr != "Google LLC") throw new Exception($"({area},{addr})");
    });

    Run("OnlineIp_ParseTencent", () =>
    {
        var json = "{\"status\":0,\"message\":\"query ok\",\"result\":{\"ip\":\"116.234.91.199\",\"location\":{\"lat\":31.23,\"lng\":121.47},\"ad_info\":{\"nation\":\"中国\",\"province\":\"上海市\",\"city\":\"上海市\",\"district\":\"杨浦区\",\"adcode\":310110},\"isp\":\"电信\"}}";
        var (area, addr) = OnlineIp.Parse(json);
        if (area != "中国–上海市–上海市–杨浦区" || addr != "电信") throw new Exception($"({area},{addr})");
    });

    Run("OnlineIp_ParseAmap", () =>
    {
        var json = "{\"status\":\"1\",\"info\":\"OK\",\"province\":\"广东省\",\"city\":\"深圳市\"}";
        var (area, addr) = OnlineIp.Parse(json);
        if (area != "广东省–深圳市" || addr != "") throw new Exception($"({area},{addr})");
    });

    Run("OnlineIp_ParseBaidu", () =>
    {
        var json = "{\"address\":\"CN|北京|北京|None|CHINANET|0|0\",\"content\":{\"address_detail\":{\"province\":\"北京市\",\"city\":\"北京市\"}},\"status\":0}";
        var (area, addr) = OnlineIp.Parse(json);
        if (area != "北京市–北京市" || addr != "CN|北京|北京|None|CHINANET|0|0") throw new Exception($"({area},{addr})");
    });

    Run("OnlineIp_ParseFail", () =>
    {
        var (area, addr) = OnlineIp.Parse("{\"status\":\"fail\",\"message\":\"invalid query\"}");
        if (area != "" || addr != "") throw new Exception($"({area},{addr})");

        (area, addr) = OnlineIp.Parse("not json");
        if (area != "" || addr != "") throw new Exception($"invalid ({area},{addr})");
    });

    Run("OnlineIp_BuildUrl", () =>
    {
        var online = new OnlineIp();
        if (online.BuildUrl("8.8.8.8") != "http://ip-api.com/json/8.8.8.8") throw new Exception("默认");

        online.Server = "https://apis.map.qq.com/ws/location/v1/ip?ip={0}&key={1}";
        online.Key = "mykey";
        if (online.BuildUrl("8.8.8.8") != "https://apis.map.qq.com/ws/location/v1/ip?ip=8.8.8.8&key=mykey") throw new Exception("带Key");
    });

    Run("OnlineIp_CacheHit", () =>
    {
        var cache = new MemoryCache();
        cache.Set("OnlineIp:8.8.8.8", "测试省\t测试运营商", 3600);

        var online = new OnlineIp { Cache = cache, Expire = 3600 };
        var (area, addr) = online.GetAddress("8.8.8.8");
        if (area != "测试省" || addr != "测试运营商") throw new Exception($"({area},{addr})");

        var (area2, addr2) = online.GetAddress("");
        if (area2 != "" || addr2 != "") throw new Exception("空输入");
    });

    // ============ INT 生态集成 ============

    Run("IpResolver_Register", () =>
    {
        IpResolver.Register();
        if (NetHelper.IpResolver is not IpResolver) throw new Exception($"IpResolver={NetHelper.IpResolver?.GetType().Name}");
        if (IpResolver.Online) throw new Exception("Online 默认应为 false");

        // 空输入走容错分支，不触发网络与数据加载
        var rs = NetHelper.IpResolver.GetAddress((IPAddress)null);
        if (rs == null) throw new Exception("null 未返回空串");
    });

    // ============ CORE-5 多数据源适配 ============

    Run("IpCsv_Convert", () =>
    {
        var csvFile = Path.Combine(temp, "dbip-test.csv");
        var dbFile = Path.Combine(temp, "dbip-test.ip");
        File.WriteAllText(csvFile, """
            start_ip,end_ip,country_code,country_name,state_prov,district,city
            0.0.3.0,0.0.3.255,CN,China,Beijing,Beijing,Beijing
            0.0.4.0,0.0.4.255,CN,China,Guangdong,Shenzhen,,Nanshan
            """);

        var count = IpCsv.Convert(csvFile, dbFile);
        if (count != 2) throw new Exception($"count={count}");

        using var db = new IpDatabase().SetFile(dbFile);
        var (area, addr) = db.GetAddress("0.0.3.100".ToUInt32IP());
        if (area != "China–Beijing–Beijing–Beijing" || addr != "") throw new Exception($"({area},{addr})");
    });

    // ============ 运行时多语言 ============

    Run("Localization_PrefixedLog", () =>
    {
        var log = new CapturingLog();
        var oldCulture = Localization.Culture;
        var oldLogCulture = Localization.LogCulture;
        try
        {
            // InvariantGlobalization 下具名区域性不可用，此时词表不注册，消息回退英文
            try { Localization.Culture = new CultureInfo("en-US"); } catch (CultureNotFoundException) { }
            try { Localization.LogCulture = new CultureInfo("zh-CN"); } catch (CultureNotFoundException) { }

            log.InfoPrefixed("[X] ", "IP database: {0}", "x");
            if (log.Last != "[X] IP database: x" && log.Last != "[X] IP数据库：x") throw new Exception($"log={log.Last}");

            log.ErrorPrefixed("[X] ", "IP record count: {0:n0}", 5);
            if (log.Last != "[X] IP record count: 5" && log.Last != "[X] IP记录数：5") throw new Exception($"log={log.Last}");
        }
        finally
        {
            Localization.Culture = oldCulture;
            Localization.LogCulture = oldLogCulture;
        }
    });
}
finally
{
    try { Directory.Delete(temp, true); } catch { }
}

var pass = results.Count(r => r.Ok);
var fail = results.Count(r => !r.Ok);
Console.WriteLine($"TOTAL pass={pass} fail={fail}");
foreach (var r in results.Where(r => !r.Ok))
{
    Console.WriteLine($"FAIL {r.Name}: {r.Error}");
}

return fail > 0 ? 1 : 0;

/// <summary>捕获日志消息的测试日志器</summary>
sealed class CapturingLog : ILog
{
    private String? _last;

    public Boolean Enable { get; set; } = true;

    public LogLevel Level { get; set; } = LogLevel.All;

    public String? Last => _last;

    public void Write(LogLevel level, String format, params Object?[] args) => _last = args != null && args.Length > 0 ? String.Format(format, args) : format;

    public void Debug(String format, params Object?[] args) => Write(LogLevel.Debug, format, args);

    public void Info(String format, params Object?[] args) => Write(LogLevel.Info, format, args);

    public void Warn(String format, params Object?[] args) => Write(LogLevel.Warn, format, args);

    public void Error(String format, params Object?[] args) => Write(LogLevel.Error, format, args);

    public void Fatal(String format, params Object?[] args) => Write(LogLevel.Fatal, format, args);
}
