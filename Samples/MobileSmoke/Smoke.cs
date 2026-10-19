using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NewLife;

namespace MobileSmoke;

/// <summary>移动端冒烟：验证主库在 Android/iOS 上可加载并执行核心 API</summary>
public static class Smoke
{
    /// <summary>执行全部检查</summary>
    /// <returns>通过数、失败数与结果文件路径</returns>
    public static (Int32 Pass, Int32 Fail, String Path) Run()
    {
        var log = new List<String>();
        var pass = 0;
        var fail = 0;

        void Check(String name, Action action)
        {
            try
            {
                action();
                log.Add($"OK {name}");
                pass++;
            }
            catch (Exception ex)
            {
                log.Add($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
                fail++;
            }
        }

        var asm = typeof(NewLife.IP.Ip).Assembly;
        Check("assembly:" + asm.GetName().Name, () =>
        {
            if (CountTypes(asm) == 0) throw new Exception("no exported types");
        });
        Check("ToUInt32IP", () => { if (NewLife.IP.IpHelper.ToUInt32IP("114.114.114.114") == 0) throw new Exception("zero"); });
        Check("ToStringIP", () => { var s = NewLife.IP.IpHelper.ToStringIP(NewLife.IP.IpHelper.ToUInt32IP("114.114.114.114")); if (s != "114.114.114.114") throw new Exception(s); });
        Check("Localization", () =>
        {
            var oldCulture = Localization.Culture;
            try
            {
                // 平台缺少 ICU 时具名区域性不可用，此时词表不注册，按英文源文回退
                var hasCulture = true;
                try { Localization.Culture = new CultureInfo("zh-CN"); } catch (CultureNotFoundException) { hasCulture = false; }

                var text = Localization.Translate("IP database: {0}", "x");
                var expected = hasCulture ? "IP数据库：x" : "IP database: x";
                if (text != expected) throw new Exception(text);
            }
            finally
            {
                Localization.Culture = oldCulture;
            }
        });
        Check("IpCsv_Multilingual", () =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                // 覆盖 GB18030 自动注册、非中文地名与超过 64 字节的长文本
                var csv = Path.Combine(dir, "dbip.csv");
                var dbFile = Path.Combine(dir, "ip.db");
                var longName = "中国–" + new String('北', 40) + "–São Paulo–القاهرة–😀";
                File.WriteAllText(csv, "start_ip,end_ip,country_code,country_name,state_prov,district,city\n" +
                    "1.0.0.0,1.0.0.255,BR,Brazil,São Paulo,São Paulo,São Paulo\n" +
                    "2.0.0.0,2.0.0.255,CN,China,Beijing,Beijing," + longName + "\n", Encoding.UTF8);

                if (NewLife.IP.IpCsv.Convert(csv, dbFile) != 2) throw new Exception("count");

                using var db = new NewLife.IP.IpDatabase().SetFile(dbFile);
                var br = db.GetAddress(NewLife.IP.IpHelper.ToUInt32IP("1.0.0.10")).area;
                if (br != "Brazil–São Paulo–São Paulo–São Paulo") throw new Exception(br);
                var cn = db.GetAddress(NewLife.IP.IpHelper.ToUInt32IP("2.0.0.10")).area;
                if (cn != "China–Beijing–Beijing–" + longName) throw new Exception(cn);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        });
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(dir);
        var file = System.IO.Path.Combine(dir, "smoke.json");
        var json = JsonSerializer.Serialize(new
        {
            pass,
            fail,
            arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            log,
        });
        File.WriteAllText(file, json);
        return (pass, fail, file);
    }

    private static Int32 CountTypes(Assembly asm)
    {
        try
        {
            return asm.GetExportedTypes().Length;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Count(e => e != null);
        }
    }
}
