using Windows.Devices.WiFiDirect.Services;

namespace wifiservive
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WiFiDirectServiceAdvertiser wiFiDirect = new WiFiDirectServiceAdvertiser("666");

            wiFiDirect.Start();
          
            Console.WriteLine("Hello, World!");

            Console.ReadKey();
        }
    }
}