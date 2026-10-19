using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NewLife;
using NewLife.IP;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>运行时多语言与带前缀日志测试：验证词表注册、日志语言选择与前缀拼接</summary>
public class LocalizationTests : IClassFixture<BigFileFixture>
{
    readonly BigFileFixture _fixture;

    public LocalizationTests(BigFileFixture fixture) => _fixture = fixture;

    static LocalizationTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 触发 NewLife.IP 程序集加载，执行源生成器的 ModuleInitializer 注册词表
        _ = typeof(Ip).Assembly;
    }

    /// <summary>InvariantGlobalization 模式下具名区域性不可用，跳过依赖译文的断言</summary>
    static void RequireCulture(params String[] names)
    {
        foreach (var name in names)
        {
            try
            {
                _ = new CultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                Assert.Skip("InvariantGlobalization 模式下不支持具名区域性，跳过多语言断言");
            }
        }
    }

    /// <summary>在指定语言下执行，结束后恢复全局语言设置</summary>
    static void WithCulture(CultureInfo culture, CultureInfo logCulture, Action action)
    {
        var oldCulture = Localization.Culture;
        var oldLogCulture = Localization.LogCulture;
        Localization.Culture = culture;
        Localization.LogCulture = logCulture;
        try
        {
            action();
        }
        finally
        {
            Localization.Culture = oldCulture;
            Localization.LogCulture = oldLogCulture;
        }
    }

    [Theory(DisplayName = "翻译表覆盖全部运行时语言")]
    [InlineData("zh-CN", "IP数据库")]
    [InlineData("zh-TW", "IP數據庫")]
    [InlineData("de", "IP-Datenbank")]
    [InlineData("es", "Base de datos de IP")]
    [InlineData("fr", "Base de données IP")]
    [InlineData("ja", "IP データベース")]
    [InlineData("ko", "IP 데이터베이스")]
    [InlineData("pt", "Banco de dados de IP")]
    [InlineData("ru", "База данных IP")]
    [InlineData("ar", "قاعدة بيانات IP")]
    public void Translate_AllLocales(String culture, String expected)
    {
        RequireCulture(culture);

        WithCulture(new CultureInfo(culture), null, () =>
        {
            var text = Localization.Translate("IP database: {0}", "x");
            Assert.Contains(expected, text);
        });
    }

    [Fact(DisplayName = "未注册语言回退英文")]
    public void Translate_Unsupported()
    {
        RequireCulture("eo");

        WithCulture(new CultureInfo("eo"), null, () =>
        {
            Assert.Equal("IP database: x", Localization.Translate("IP database: {0}", "x"));
        });
    }

    [Fact(DisplayName = "带前缀日志先翻译消息再拼前缀")]
    public void LogPrefixed_Translate()
    {
        RequireCulture("en-US", "zh-CN");

        // Culture 走英文，日志语言单独指定中文，验证 LogCulture 优先生效
        WithCulture(new CultureInfo("en-US"), new CultureInfo("zh-CN"), () =>
        {
            var log = new CapturingLog();

            log.InfoPrefixed("[X] ", "IP database: {0}", "x");
            Assert.Equal("[X] IP数据库：x", log.Last);

            log.ErrorPrefixed("[X] ", "IP record count: {0:n0}", 5);
            Assert.Equal("[X] IP记录数：5", log.Last);
        });
    }

    [Fact(DisplayName = "未注册语言的前缀日志回退英文")]
    public void LogPrefixed_Fallback()
    {
        RequireCulture("eo");

        WithCulture(new CultureInfo("eo"), null, () =>
        {
            var log = new CapturingLog();

            log.InfoPrefixed("[X] ", "IP database: {0}", "x");
            Assert.Equal("[X] IP database: x", log.Last);
        });
    }

    [Fact(DisplayName = "简体中文词表覆盖全部运行时消息")]
    public void Translate_AllKeys_zhHans()
    {
        RequireCulture("zh-CN");

        WithCulture(new CultureInfo("zh-CN"), null, () =>
        {
            Assert.Equal("找不到CSV文件", Localization.Translate("CSV file not found."));
            Assert.Equal("CSV中没有有效记录", Localization.Translate("No valid records in the CSV file."));
            Assert.Equal("无法找到IP数据库x", Localization.Translate("Unable to find the IP database {0}", "x"));
            Assert.Equal("IP数据库：x", Localization.Translate("IP database: {0}", "x"));
            Assert.Equal("没有找到IP数据库x，准备联网获取 y", Localization.Translate("IP database {0} was not found; downloading from {1}.", "x", "y"));
            Assert.Equal("IP数据库x已过期，准备联网更新 y", Localization.Translate("IP database {0} has expired; downloading an update from {1}.", "x", "y"));
            Assert.Equal("下载IP数据库成功，文件：x", Localization.Translate("IP database downloaded successfully: {0}", "x"));
            Assert.Equal("下载IP数据库失败，文件不存在！x", Localization.Translate("Failed to download the IP database; the file does not exist: {0}", "x"));
            Assert.Equal("IP记录数：5", Localization.Translate("IP record count: {0:n0}", 5));
            Assert.Equal("IP数据库转换完成：1 条记录 → x", Localization.Translate("IP database conversion completed: {0:n0} records → {1}", 1, "x"));
        });
    }

    [Fact(DisplayName = "Ip 自定义日志与前缀，消息按日志语言翻译")]
    public void Ip_LogPrefixed()
    {
        RequireCulture("en-US", "zh-CN");

        WithCulture(new CultureInfo("en-US"), new CultureInfo("zh-CN"), () =>
        {
            var log = new CapturingLog();
            var ip = new Ip { DbFile = _fixture.File, Log = log, LogPrefix = "[Mine] " };
            try
            {
                Assert.True(ip.Init());

                var messages = log.Messages;
                Assert.Contains(messages, e => e.StartsWith("[Mine] ") && e.Contains("IP数据库："));
                // 数据库记录数日志继承 Ip 的日志对象与前缀
                Assert.Contains(messages, e => e.StartsWith("[Mine] ") && e.Contains("IP记录数"));
                // 前缀参与整串查表会回退英文，此处必须已翻译
                Assert.DoesNotContain(messages, e => e.Contains("IP database:"));
            }
            finally
            {
                ip.Db?.Dispose();
            }
        });
    }

    [Fact(DisplayName = "IpDatabase 默认前缀写日志")]
    public void IpDatabase_LogPrefixed()
    {
        RequireCulture("en-US", "zh-CN");

        WithCulture(new CultureInfo("en-US"), new CultureInfo("zh-CN"), () =>
        {
            var log = new CapturingLog();
            using var db = new IpDatabase { Log = log };
            db.SetFile(_fixture.File);

            Assert.Contains(log.Messages, e => e.StartsWith("[IpDatabase] ") && e.Contains("IP记录数"));
        });
    }

    [Fact(DisplayName = "IpCsv 指定日志对象后按前缀写日志")]
    public void IpCsv_LogPrefixed()
    {
        RequireCulture("en-US", "zh-CN");

        WithCulture(new CultureInfo("en-US"), new CultureInfo("zh-CN"), () =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "XUnitTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var csvFile = Path.Combine(dir, "dbip.csv");
                var dbFile = Path.Combine(dir, "dbip.ip");
                File.WriteAllText(csvFile, "start_ip,end_ip,country_code,country_name,state_prov,district,city\n" +
                    "1.0.0.0,1.0.0.255,CN,China,Beijing,,Beijing\n", Encoding.UTF8);

                var log = new CapturingLog();
                var count = IpCsv.Convert(csvFile, dbFile, log);

                Assert.Equal(1, count);
                Assert.Contains(log.Messages, e => e.StartsWith("[IpCsv] ") && e.Contains("IP数据库转换完成"));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        });
    }
}

/// <summary>捕获日志消息的测试日志器</summary>
public sealed class CapturingLog : ILog
{
    private readonly List<String> _messages = [];

    public Boolean Enable { get; set; } = true;

    public LogLevel Level { get; set; } = LogLevel.All;

    public IReadOnlyList<String> Messages
    {
        get { lock (_messages) return _messages.ToArray(); }
    }

    public String Last
    {
        get { lock (_messages) return _messages.Count > 0 ? _messages[_messages.Count - 1] : null; }
    }

    public void Write(LogLevel level, String format, params Object[] args) => Add(args != null && args.Length > 0 ? String.Format(format, args) : format);

    public void Debug(String format, params Object[] args) => Write(LogLevel.Debug, format, args);

    public void Info(String format, params Object[] args) => Write(LogLevel.Info, format, args);

    public void Warn(String format, params Object[] args) => Write(LogLevel.Warn, format, args);

    public void Error(String format, params Object[] args) => Write(LogLevel.Error, format, args);

    public void Fatal(String format, params Object[] args) => Write(LogLevel.Fatal, format, args);

    void Add(String message)
    {
        lock (_messages) _messages.Add(message);
    }
}
