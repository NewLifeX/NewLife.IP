using NewLife;
using NewLife.IP;
using NewLife.Log;

namespace Test;

class Program
{
    static void Main(String[] args)
    {
        XTrace.UseConsole();

        try
        {
            //TestHyperLogLog();
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
        //NetHelper.IpResolver = new IpResolver();
        IpResolver.Register();

        var ip = "47.100.59.126";
        var addr = ip.IPToAddress();
        XTrace.WriteLine(addr);
    }
}