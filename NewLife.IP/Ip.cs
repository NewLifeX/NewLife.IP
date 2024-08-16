using System.Net;
using System.Text;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.IP;

/// <summary>IP搜索</summary>
public class Ip
{
    private IpDatabase _zip;

    /// <summary>数据文件</summary>
    public String DbFile { get; set; } = "";

    /// <summary>数据库实例</summary>
    public IpDatabase Db => _zip;

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
            if (fi == null || !fi.Exists || fi.Length < 3 * 1024 * 1024 || fi.LastWriteTime < new DateTime(2024, 08, 05))
            {
                var task = Task.Run(() => Download(ip));
                // 静态构造函数里不能等待，否则异步函数也无法执行
                task.Wait(5_000);
            }

            var zip = new IpDatabase();
            ip = DbFile;
            if (!File.Exists(ip))
            {
                XTrace.WriteLine("无法找到IP数据库{0}", ip);
                return false;
            }
            XTrace.WriteLine("IP数据库：{0}", ip);

            try
            {
                zip.SetFile(ip);

                _zip = zip;
            }
            catch (Exception ex)
            {
                _inited = false;
                XTrace.WriteException(ex);

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
            XTrace.WriteLine("没有找到IP数据库{0}，准备联网获取 {1}", ip, url);
        else
            XTrace.WriteLine("IP数据库{0}已过期，准备联网更新 {1}", ip, url);

        // 无法下载ip地址库时，不要抛出异常影响业务层
        try
        {
            var client = new WebClientX
            {
                Timeout = 60_000,
                Log = XTrace.Log
            };
            var file = client.DownloadLink(url, "ip.gz", Path.GetTempPath());

            if (File.Exists(file))
            {
                if (File.Exists(ip)) File.Delete(ip);
                ip.EnsureDirectory(true);
                File.Move(file, ip);

                DbFile = ip;

                // 下载成功时，让它重新初始化
                _inited = null;

                return true;
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
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