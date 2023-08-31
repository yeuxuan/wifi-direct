

using System.Collections.Concurrent;
using Windows.Devices.WiFiDirect;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Popups;

namespace ConsoleApp27
{
    internal class Program
    {
        static ConcurrentDictionary<StreamSocketListener, WiFiDirectDevice> _pendingConnections = new ConcurrentDictionary<StreamSocketListener, WiFiDirectDevice>();
        static WiFiDirectAdvertisementPublisher wiFiDirectAdvertisementPublisher;
        static WiFiDirectConnectionListener _listener;
        static void Main(string[] args)
        {

    

            wiFiDirectAdvertisementPublisher = new WiFiDirectAdvertisementPublisher();


            wiFiDirectAdvertisementPublisher.Advertisement.ListenStateDiscoverability = WiFiDirectAdvertisementListenStateDiscoverability.Normal;

            wiFiDirectAdvertisementPublisher.Advertisement.IsAutonomousGroupOwnerEnabled = true; ;

            wiFiDirectAdvertisementPublisher.Advertisement.LegacySettings.IsEnabled = true;

            var creds = new Windows.Security.Credentials.PasswordCredential();
            creds.Password = "123456789";
            wiFiDirectAdvertisementPublisher.Advertisement.LegacySettings.Passphrase = creds;


            wiFiDirectAdvertisementPublisher.Advertisement.LegacySettings.Ssid = "测试123";


            wiFiDirectAdvertisementPublisher.StatusChanged += (x, y) =>
            {

                Console.WriteLine("wifi发生了变化");
            };
            


            wiFiDirectAdvertisementPublisher.Start();

            if (wiFiDirectAdvertisementPublisher.Status == WiFiDirectAdvertisementPublisherStatus.Started)
            {
                Console.WriteLine("开启成功");
                _listener = new WiFiDirectConnectionListener();
                _listener.ConnectionRequested += (WiFiDirectConnectionListener x,WiFiDirectConnectionRequestedEventArgs  y) =>
                {
                    
                    Console.WriteLine("有人连接了wifi");
                    
                    WiFiDirectConnectionRequest connectionRequest = y.GetConnectionRequest();



                    bool success =  Task.Run(async () =>
                    {
                        return await HandleConnectionRequestAsync(connectionRequest);
                    }).Result;

                    if (success)
                    {
                         Console.Out.WriteLine("执行成功！！！");
                    }

                };

            }
            else
            {
                Console.WriteLine("开启失败");
            }


            Console.ReadKey();
        }

        private static async Task<bool> HandleConnectionRequestAsync(WiFiDirectConnectionRequest connectionRequest)
        {

            WiFiDirectConnectionParameters wiFiDirectConnection = new WiFiDirectConnectionParameters();

            string deviceName = connectionRequest.DeviceInformation.Name;
    
            WiFiDirectDevice? wfdDevice = null;

            try
            {

                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                wfdDevice =await WiFiDirectDevice.FromIdAsync(connectionRequest.DeviceInformation.Id, connectionParameters: wiFiDirectConnection);
            }
            catch (Exception ex)
            {
                return false;
            }

            if (wfdDevice == null)
            {
                return false;
            }


            // Register for the ConnectionStatusChanged event handler
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            var listenerSocket = new StreamSocketListener();

            // Save this (listenerSocket, wfdDevice) pair so we can hook it up when the socket connection is made.
            _pendingConnections[listenerSocket] = wfdDevice;

            var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();

            listenerSocket.ConnectionReceived += OnSocketConnectionReceived;

            listenerSocket.Control.KeepAlive = false;

            try
            {
                await listenerSocket.BindServiceNameAsync("50001");
                 //listenerSocket.BindEndpointAsync(new HostName("0"), "0").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;

        }

        private static void OnSocketConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Console.WriteLine("收到信息");
        }

        private static void OnConnectionStatusChanged(WiFiDirectDevice sender, object args)
        {
            Console.WriteLine(  "连接状态变化");
        }
    }
}