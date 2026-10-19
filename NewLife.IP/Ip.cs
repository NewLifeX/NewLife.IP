using System.Net;
using System.Text;
using NewLife.Log;
using NewLife.Web;
using NewLife;

namespace NewLife.IP;

/// <summary>IP搜索</summary>
public class Ip
{
    private IpDatabase _zip;
    private ILog _log;

    /// <summary>数据文件</summary>
    public String DbFile { get; set; } = "";

    /// <summary>数据库实例</summary>
    public IpDatabase Db => _zip;

    /// <summary>日志对象。默认 <see cref="XTrace.Log"/>，设为 <see cref="Logger.Null"/> 可关闭日志</summary>
    public ILog Log { get => _log ??= XTrace.Log; set => _log = value; }

    /// <summary>日志前缀。前缀在消息翻译之后拼接，默认“[IP] ”</summary>
    public String LogPrefix { get; set; } = "[IP] ";

    static Ip()
    {
#if NETCOREAPP
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
    }

    private Boolean? _inited;
    /// <summary>初始化</summary>
    /// <returns></returns>
    public Boolean Init()
    {
        if (_inited != null) return _inited.Value;
        lock (typeof(Ip))
        {
            if (_inited != null) return _inited.Value;

            var ip = DbFile;
            if (ip.IsNullOrEmpty())
            {
                var set = Setting.Current;
                ip = set.DataPath.CombinePath("ip.gz").GetBasePath();
                if (File.Exists(ip)) DbFile = ip;
            }

            // 如果本地没有IP数据库，则从网络下载
            var fi = ip.IsNullOrWhiteSpace() ? null : ip.AsFile();
            if (fi == null || !fi.Exists || fi.Length < 3 * 1024 * 1024 || fi.LastWriteTime < new DateTime(2025, 02, 02))
            {
                var task = Task.Factory.StartNew(() => Download(ip), TaskCreationOptions.LongRunning);
                // 静态构造函数里不能等待，否则异步函数也无法执行
                task.Wait(5_000);
            }

            var zip = new IpDatabase { Log = Log, LogPrefix = LogPrefix };
            ip = DbFile;
            if (!File.Exists(ip))
            {
                Log?.InfoPrefixed(LogPrefix, "Unable to find the IP database {0}", ip);
                return false;
            }
            Log?.InfoPrefixed(LogPrefix, "IP database: {0}", ip);

            try
            {
                zip.SetFile(ip);

                _zip = zip;
            }
            catch (Exception ex)
            {
                _inited = false;
                Log?.ErrorPrefixed(LogPrefix, "{0}", ex);

                return false;
            }
        }

        _inited = true;
        return true;
    }

    private Boolean Download(String ip)
    {
        var set = Setting.Current;
        var url = set.PluginServer;

        var fi = ip.AsFile();
        if (fi == null || !fi.Exists)
            Log?.InfoPrefixed(LogPrefix, "IP database {0} was not found; downloading from {1}.", ip, url);
        else
            Log?.InfoPrefixed(LogPrefix, "IP database {0} has expired; downloading an update from {1}.", ip, url);

        // 无法下载ip地址库时，不要抛出异常影响业务层
        try
        {
            var client = new WebClientX
            {
                Timeout = 60_000,
                Log = Log
            };
            var file = client.DownloadLink(url, "ip.gz", Path.GetTempPath());

            if (!file.IsNullOrEmpty() && File.Exists(file))
            {
                Log?.InfoPrefixed(LogPrefix, "IP database downloaded successfully: {0}", file);

                if (File.Exists(ip)) File.Delete(ip);
                ip.EnsureDirectory(true);
                File.Move(file, ip);

                DbFile = ip;

                // 下载成功时，让它重新初始化
                _inited = null;

                return true;
            }
            else
                Log?.InfoPrefixed(LogPrefix, "Failed to download the IP database; the file does not exist: {0}", file);
        }
        catch (Exception ex)
        {
            Log?.ErrorPrefixed(LogPrefix, "{0}", ex);
        }

        return false;
    }

    /// <summary>获取IP地址所映射的物理地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public (String area, String addr) GetAddress(String ip)
    {
        if (String.IsNullOrEmpty(ip)) return ("", "");

        if (!Init() || _zip == null) return ("", "");

        return _zip.GetAddress(ip.Trim().ToUInt32IP());
    }

    /// <summary>获取IP地址所映射的物理地址</summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public String GetAddress(IPAddress ip)
    {
        if (ip == null) return "";

        if (!Init() || _zip == null) return "";

        var (area, addr) = _zip.GetAddress(ip.ToUInt32());
        //return area.TrimStart("中国\u2013").Replace("\u2013", null) + " " + addr;
        return area + " " + addr;
    }
}