using System.Net;
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

        Console.WriteLine("OK!");
        Console.ReadKey();
    }

    static void Test1()
    {
        IpResolver.Register();

        var ip = "47.100.59.126";
        var addr = ip.IPToAddress();
        XTrace.WriteLine(addr);
    }

    static void Test2()
    {
        Console.WriteLine("Test2");
        //Console.ReadKey();

        var ip = new Ip();
        ip.Init();

        var db = ip.Db;

        for (var idx = 0u; idx < db.Count; idx++)
        {
            var (set, addr, area) = db.GetIndex(idx);

            //if (idx % 1000 == 0)
            if (addr.Contains("纯真") || addr.Contains("CZ") || area.Contains("纯真") || area.Contains("CZ"))
            {
                XTrace.WriteLine("{0} {1} {2} {3}\t{4}", idx, set.Start.ToStringIP(), set.End.ToStringIP(), addr, area);
            }
        }
        //Console.WriteLine("Test2");
        //Console.ReadKey();
    }
}