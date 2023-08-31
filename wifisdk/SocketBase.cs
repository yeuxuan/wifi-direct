using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ConsoleApp27
{
        /// <summary>
        ///     Socket客户端/服务端 工厂
        /// </summary>
        public class SocketFactory
        {
            public static SocketBase CreatInkSocket(bool isServer, string ip, string serviceName)
            {
                return isServer ? (SocketBase)new ServerSocket(ip, serviceName) : new ClientSocket(ip, serviceName);
            }
        }


        public abstract class SocketBase
        {
            /// <summary>
            ///     客户端列表
            /// </summary>
            protected List<StreamSocket> ClientSockets;

            /// <summary>
            ///     用于连接或监听的ip地址
            /// </summary>
            protected string IpAddress;

            /// <summary>
            ///     标识是否为服务端
            /// </summary>
            protected bool IsServer;

            /// <summary>
            ///     新消息到达通知
            /// </summary>
            public Action<MessageModel> MsgReceivedAction;

            /// <summary>
            ///     服务端启动监听/客户端启动连接   失败时的通知
            /// </summary>
            public Action<Exception> OnStartFailed;

            /// <summary>
            ///     服务端启动监听/客户端启动连接   成功时的通知
            /// </summary>
            public Action OnStartSuccess;

            /// <summary>
            ///     连接或监听的端口号
            /// </summary>
            protected string RemoteServiceName;

            /// <summary>
            ///     客户端Socket对象
            /// </summary>
            protected StreamSocket Socket;

            /// <summary>
            ///     是否在监听端口/是否和服务端在保持着连接
            /// </summary>
            public bool Working;

            /// <summary>
            ///     客户端/服务端 流写入器
            /// </summary>
            protected DataWriter Writer;

            /// <summary>
            ///     客户端连接到服务器  /  服务端启动监听
            /// </summary>
            /// <returns></returns>
            public abstract Task Start();

            /// <summary>
            ///     客户端断开连接  /  服务端停止监听
            /// </summary>
            public abstract void Dispose();

            /// <summary>
            ///     发送消息
            /// </summary>
            /// <param name="msg">消息对象</param>
            /// <param name="client">客户端Client对象</param>
            /// <returns></returns>
            public async Task SendMsg(MessageModel msg, StreamSocket client = null)
            {
                if (msg != null)
                {
                    await SendData(client, JsonConvert.SerializeObject(msg));
                }
            }

            protected async Task SendData(StreamSocket client, string data)
            {
                try
                {
                    if (!Working) return;
                    if (string.IsNullOrEmpty(data)) return;

                    if (!IsServer)
                    {
                        if (Writer == null)
                        {
                            Writer = new DataWriter(Socket.OutputStream);
                        }
                        await WriterData(data);
                    }

                    else if (IsServer)
                    {
                        foreach (var clientSocket in ClientSockets.Where(s => s != client))
                        {
                            try
                            {
                                Writer = new DataWriter(clientSocket.OutputStream);
                                await WriterData(data);
                                //分离流 防止OutputStream对象被释放
                                Writer.DetachStream();
                            }
                            catch (Exception)
                            {
                                //
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                    {
                        throw;
                    }

                    Debug.WriteLine("Send failed with error: " + exception.Message);
                }
            }

            private async Task WriterData(string data)
            {
                //转成 byte[] 发送
                var bytes = Encoding.UTF8.GetBytes(data);
                //先写入数据的长度
                Writer.WriteInt32(bytes.Length);
                //写入数据
                Writer.WriteBytes(bytes);
                await Writer.StoreAsync();
                Debug.WriteLine("Data sent successfully.");
            }
        }

    }