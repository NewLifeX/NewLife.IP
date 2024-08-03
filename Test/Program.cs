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
        var ip = new Ip();
        ip.Init();

        var db = ip.Db;

        for (var idx = 0u; idx < db.Count; idx++)
        {
            var (set, addr) = db.GetIndex(idx);

            if (idx % 100 == 0)
            {
                XTrace.WriteLine("{0}\t{1} - {2}\t{3}", idx, set.Start.ToStringIP(), set.End.ToStringIP(), addr);
            }
        }
    }
}