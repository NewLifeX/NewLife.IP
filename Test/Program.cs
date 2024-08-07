using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using NewLife;
using NewLife.IP;
using NewLife.Log;
using NewLife.Reflection;

namespace Test;

class Program
{
    static void Main(String[] args)
    {
        Runtime.CreateConfigOnMissing = false;
        XTrace.UseConsole();

        try
        {
            Test2();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        if (Runtime.Windows)
        {
            var p = Process.GetCurrentProcess();
            EmptyWorkingSet(p.Handle);
        }

        Console.WriteLine("OK!");
        Console.ReadKey();
    }

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern Boolean EmptyWorkingSet(IntPtr hProcess);

    static void Test1()
    {
        IpResolver.Register();

        var ip = "47.100.59.126";
        var addr = ip.IPToAddress();
        XTrace.WriteLine(addr);
    }

    static void Test2()
    {
        Console.WriteLine("Start");
        Console.ReadKey();

        var ip = new Ip();
        ip.Init();

        var db = ip.Db;

        for (var idx = 0u; idx < db.Count; idx++)
        {
            var (set, area, addr) = db.GetIndex(idx);

            if (idx % 10000 == 0)
            //if (addr.Contains("纯真") || addr.Contains("CZ") || area.Contains("纯真") || area.Contains("CZ"))
            {
                XTrace.WriteLine("{0} {1} {2} {3}\t{4}", idx, set.Start.ToStringIP(), set.End.ToStringIP(), area, addr);
            }
        }
        Console.WriteLine("End");
        Console.ReadKey();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
    }
}