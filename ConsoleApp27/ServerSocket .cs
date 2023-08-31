using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ConsoleApp27
{
    public class ServerSocket : SocketBase
    {
        /// <summary>
        /// Socket监听器
        /// </summary>
        protected StreamSocketListener Listener;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">ip</param>
        /// <param name="remoteServiceName">端口号</param>
        public ServerSocket(string ip, string remoteServiceName)
        {
            IpAddress = ip;
            RemoteServiceName = remoteServiceName;
        }

        /// <summary>
        /// 启动监听
        /// </summary>
        /// <returns></returns>
        public override async Task Start()
        {
            try
            {
                if (Working) return;
                IsServer = true;
                ClientSockets = new List<StreamSocket>();
                Listener = new StreamSocketListener()
                {
                    Control = { KeepAlive = false }
                };
                Listener.ConnectionReceived += OnConnection;  //新连接接入时的事件
                await Listener.BindEndpointAsync(new HostName(IpAddress), RemoteServiceName);
                Working = true;
                OnStartSuccess?.Invoke();
            }
            catch (Exception exc)
            {
                OnStartFailed?.Invoke(exc);
            }
        }

        private async void OnConnection(StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Writer = null;
            //获取新接入的Socket的InputStream 来读取远程目标发来的数据
            var reader = new DataReader(args.Socket.InputStream);

            //添加一个StreamSocket客户端到 客户端列表
            ClientSockets.Add(args.Socket);
            try
            {
                while (Working)
                {
                    //等待数据进来
                    var sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                    if (sizeFieldCount != sizeof(uint))
                    {
                        reader.DetachStream();
                        reader.Dispose();
                        //主动断开连接
                        ClientSockets?.Remove(args.Socket);
                        return;
                    }

                    var stringLength = reader.ReadUInt32();
                    //先获取数据的长度
                    var actualStringLength = await reader.LoadAsync(stringLength);
                    if (stringLength != actualStringLength)
                    {
                        //数据接收中断开连接
                        reader.DetachStream();
                        reader.Dispose();
                        ClientSockets?.Remove(args.Socket);
                        return;
                    }

                    var dataArray = new byte[actualStringLength];
                    //根据数据长度获取数据
                    reader.ReadBytes(dataArray);
                    //转为json数据字符串
                    var dataJson = Encoding.UTF8.GetString(dataArray);
                    //反序列化数据为对象
                    var data = JsonConvert.DeserializeObject<MessageModel>(dataJson);
                    //给所有客户端发送数据
                    await SendMsg(data, args.Socket);
                    //触发新消息到达Action
                    MsgReceivedAction?.Invoke(data);
                }
            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    //
                }
                //Debug.WriteLine(string.Format("Received data: \"{0}\"",
                //    "Read stream failed with error: " + exception.Message));
                reader.DetachStream();
                reader.Dispose();
                ClientSockets?.Remove(args.Socket);
            }
        }

        public async override void Dispose()
        {
            Working = false;
            //给所有客户端发送断开服务的消息
            await SendMsg(new MessageModel
            {
                MessageType = "断了"
            });
            foreach (var clientSocket in ClientSockets)
            {
                clientSocket.Dispose();
            }
            ClientSockets.Clear();
            ClientSockets = null;

            Listener.ConnectionReceived -= OnConnection;
            Listener?.CancelIOAsync();
            Listener.Dispose();
            Listener = null;
        }
    }
}
