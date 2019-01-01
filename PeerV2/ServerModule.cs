using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace light.asynctcp
{
    enum LogType
    {
        Message,
        Warning,
        Error
    }
    interface ILogger
    {
        void Log(string txt, LogType type = LogType.Message);
    }
    class ConsoleLogger : ILogger
    {
        public void Log(string txt, LogType type = LogType.Message)
        {
            if (type == LogType.Warning)
                Console.Write("<Warn>");
            else if (type == LogType.Error)
                Console.Write("<Error>");

            Console.WriteLine(txt);
        }
    }
    partial class ServerModule : allpet.peer.tcp.IPeer
    {

        public static UInt64 moduleID = 0;
        /// <summary>  
        /// 监听Socket，用于接受客户端的连接请求  
        /// </summary>  
        private Socket socketListen;

        public event Action<ulong, IPEndPoint> OnAccepted;
        public event Action<ulong> OnConnected;
        public event Action<ulong, Exception> OnLinkError;
        public event Action<ulong, byte[]> OnRecv;
        public event Action<ulong> OnClosed;

        public ILogger logger;

        public UInt64 ID
        {
            get;
            private set;
        }
        public ServerModule()
        {
            this.ID = moduleID++;
        }
        public void Start(allpet.peer.tcp.PeerOption option)
        {
            logger = new ConsoleLogger();

            logger.Log("Module Start==");

            int capacity = 1000;
            InitPools(capacity);
            InitProcess();

            logger.Log("==Module Start");
        }
        public void Close()
        {

        }
        public void Dispose()
        {
            this.Close();
        }
        //监听
        public void Listen(IPEndPoint endPoint)
        {
            if (this.OnAccepted == null)
            {
                throw new Exception("need set event OnAccepted");
            }
            if (this.OnClosed == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnRecv == null)
            {
                throw new Exception("need set event OnRecv");
            }
            logger.Log("Module listen==");

            if (this.socketListen != null)
            {
                throw new Exception("already in listen");
            }
            socketListen = new Socket(SocketType.Stream, ProtocolType.Tcp);
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socketListen.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                socketListen.Bind(new IPEndPoint(IPAddress.IPv6Any, endPoint.Port));
            }
            else
            {
                socketListen.Bind(endPoint);
            }
            socketListen.Listen(10000);

            StartAccept(null);

            logger.Log("==Module listen");



        }
        public void StopListen()
        {
            socketListen.Close();
        }
        void StartAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = GetFreeEventArgs();
            }
            args.AcceptSocket = null;
            if (!socketListen.AcceptAsync(args))
            {
                ProcessAccept(args);
            }

            //_maxAcceptedClients.WaitOne();

            //不断执行检查是否有无效连接
            //var thread = new System.Threading.Thread(_DaemonThread);
            //thread.IsBackground = true;
            //thread.Start();
        }
        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                //logger.Log("got complete state:" + e.LastOperation + "|" + e.SocketError);

                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Accept:
                        ProcessAccept(e);
                        break;
                    case SocketAsyncOperation.Connect:
                        ProcessConnect(e, e.UserToken as LinkInfo);
                        break;
                    case SocketAsyncOperation.Disconnect:
                        ProcessDisConnect(e, e.UserToken as LinkInfo);
                        break;
                    case SocketAsyncOperation.Receive:
                        {
                            if (e.SocketError != SocketError.Success)
                            {
                                throw new Exception("what the fuck.");
                            }
                            else
                            {
                                bool bEnd = false;
                                while (!bEnd)
                                {
                                    bEnd = ProcessReceice(e, e.UserToken as LinkInfo);
                                }
                                //ProcessReceice(e, e.UserToken as LinkInfo);
                            }
                        }
                        break;
                    case SocketAsyncOperation.Send:
                        ProcessSend(e, e.UserToken as LinkInfo);
                        break;
                }
            }
            catch (Exception Err)
            {
                Console.WriteLine("error:" + Err.Message +"|"+ Err.StackTrace);
            }
        }

        //void _DaemonThread()
        //{
        //    while (true)
        //    {
        //        //加上超时检测代码

        //        for (int i = 0; i < 60 * 1000 / 10; i++) //每分钟检测一次
        //        {
        //            //if (!m_thread.IsAlive)
        //            //    break;
        //            Thread.Sleep(10);
        //        }
        //    }
        //}

        public UInt64 Connect(IPEndPoint linktoEndPoint)
        {
            if (this.OnConnected == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnLinkError == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnClosed == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnRecv == null)
            {
                throw new Exception("need set event OnRecv");
            }
            var eventArgs = GetFreeEventArgs();
            LinkInfo link = GetFreeLink();
            eventArgs.UserToken = link;
            link.type = LinkType.ConnectedLink;
            link.Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            link.Handle = (UInt64)link.Socket.Handle;
            link.sendTag = 0;
            link.ConnectDateTime = DateTime.Now;
            this.links[link.Handle] = link;

            eventArgs.RemoteEndPoint = linktoEndPoint;
            if (!link.Socket.ConnectAsync(eventArgs))
            {
                ProcessConnect(eventArgs, link);
            }

            return link.Handle;
        }

        public void Send(ulong linkid, byte[] data)
        {
            var link = this.links[linkid];
            var oncecount = _SendBufferSize;

            var last = data.Length % oncecount;
            var splitcount = data.Length / oncecount;
            for (var i = 0; i < splitcount; i++)
            {
                SendOnce(link, new ArraySegment<byte>(data, i * oncecount, oncecount));
            }
            if (last != 0)
                SendOnce(link, new ArraySegment<byte>(data, splitcount * oncecount, last));
        }
        public void SendPacket(ulong linkid, byte[] data)
        {
            if (data.Length > 0xffff)
            {
                throw new Exception(" do not allow msg packet big than 0xffff.");
            }
            Send(linkid, BitConverter.GetBytes((UInt16)data.Length));
            Send(linkid, data);
        }

        public void Disconnect(ulong linkid)
        {
            var link = this.links[linkid];
            try
            {
                var e = GetFreeEventArgs();
                e.UserToken = link;
                var b = link.Socket.DisconnectAsync(e);
                //if (!b)
                //{
                //    ProcessDisConnect(e, link);
                //}
                link.Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                // Throw if client has closed, so it is not necessary to catch.
                //Log4Debug(ex.StackTrace);
            }
            finally
            {
                //link.Socket.Close();
                //link.Socket = null;
            }
        }
    }
}
