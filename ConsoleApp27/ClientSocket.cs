using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Newtonsoft.Json;

namespace ConsoleApp27
{
    public class ClientSocket : SocketBase
    {
        private HostName _hostName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">目标ip</param>
        /// <param name="remoteServiceName">端口号</param>
        public ClientSocket(string ip, string remoteServiceName)
        {
            IsServer = false;
            IpAddress = ip;
            RemoteServiceName = remoteServiceName;
        }

        /// <summary>
        /// 开始连接到服务端
        /// </summary>
        /// <returns></returns>
        public override async Task Start()
        {
            try
            {
                if (Working) return;
                _hostName = new HostName(IpAddress);
                //初始化StreamSocket对象
                Socket = new StreamSocket();
                Socket.Control.KeepAlive = false;

                await Console.Out.WriteLineAsync("Connecting to: " + _hostName.DisplayName);
                //开始连接目标计算机
                await Socket.ConnectAsync(_hostName, RemoteServiceName);
                OnStartSuccess?.Invoke();
                await Console.Out.WriteLineAsync("Connected");
                Working = true;
                await Task.Run(async () =>
                {
                    //创建一个读取器 来读取服务端发送来的数据
                    var reader = new DataReader(Socket.InputStream);

                    try
                    {
                        while (Working)
                        {
                            var sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                            if (sizeFieldCount != sizeof(uint))
                            {
                                //主动断开连接
                                reader.DetachStream();
                                OnStartFailed?.Invoke(new Exception("断开连接"));
                                Dispose();
                                return;
                            }

                            var stringLength = reader.ReadUInt32();
                            var actualStringLength = await reader.LoadAsync(stringLength);
                            if (stringLength != actualStringLength)
                            {
                                //数据接收中断开连接
                                reader.DetachStream();
                                OnStartFailed?.Invoke(new Exception("断开连接"));
                                Dispose();
                                return;
                            }
                            //接受数据
                            var dataArray = new byte[actualStringLength];
                            reader.ReadBytes(dataArray);
                            //转为json字符串
                            var dataJson = Encoding.UTF8.GetString(dataArray);
                            //反序列化为数据对象
                            var data = JsonConvert.DeserializeObject<MessageModel>(dataJson);
                            //新消息到达通知
                            MsgReceivedAction?.Invoke(data);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (Windows.Networking.Sockets.SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                        {
                        }
                        await Console.Out.WriteLineAsync(string.Format("Received data: \"{0}\"",
                            "Read stream failed with error: " + exception.Message));
                        reader.DetachStream();
                        OnStartFailed?.Invoke(exception);
                        Dispose();
                    }
                });
            }
            catch (Exception exception)
            {
                if (Windows.Networking.Sockets.SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                }
                Debug.WriteLine(string.Format("Received data: \"{0}\"",
                    "Read stream failed with error: " + exception.Message));
                OnStartFailed?.Invoke(exception);
                Dispose();
            }
        }

        public override void Dispose()
        {
            Working = false;
            Writer = null;
            Socket?.Dispose();
            Socket = null;
        }
    }
}
