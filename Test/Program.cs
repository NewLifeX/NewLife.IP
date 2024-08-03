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
            Test1();
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

        var ipr = (NetHelper.IpResolver as IpResolver).GetValue("_ip") as Ip;
        var db = ipr.GetValue("_zip") as IpDatabase;

        for (var i = db.Start; i < db.End; i++)
        {
            addr = db.GetAddress(i);

            if (i % 10000 == 0)
            {
                var ip2 = new IPAddress(i);
                var buf = ip2.GetAddressBytes();
                Array.Reverse(buf);
                XTrace.WriteLine("{0}\t{1:000}.{2:000}.{3:000}.{4:000}\t{5}", i - db.Start, buf[0], buf[1], buf[2], buf[3], addr);
            }
        }
    }
}