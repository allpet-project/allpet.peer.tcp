using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace light.asynctcp
{
    public partial class ServerModule : allpet.peer.tcp.IPeer
    {


        /// <summary>  
        /// 监听Socket，用于接受客户端的连接请求  
        /// </summary>  
        private Socket socketListen;

        public event Action<ulong, IPEndPoint> OnAccepted;
        public event Action<ulong> OnConnected;
        public event Action<ulong, Exception> OnLinkError;
        public event Action<ulong, byte[]> OnRecv;
        public event Action<ulong> OnCloseed;

        public void Start(allpet.peer.tcp.PeerOption option)
        {
            int capacity = 1000;
            InitEventArgsPool(capacity);
            InitProcess();

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


        }
        public void StopListen()
        {

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

            _maxAcceptedClients.WaitOne();

            //不断执行检查是否有无效连接
            var thread = new System.Threading.Thread(_DaemonThread);
            thread.IsBackground = true;
            thread.Start();
        }
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                //lock (AcceptLocker)//这个线程锁要检查是否必要
                {
                    ProcessAccept(e);
                }
            }
            catch (Exception Err)
            {
                Console.WriteLine("error:" + Err.Message);
            }
        }

        void _DaemonThread()
        {
            while (true)
            {
                //加上超时检测代码

                for (int i = 0; i < 60 * 1000 / 10; i++) //每分钟检测一次
                {
                    //if (!m_thread.IsAlive)
                    //    break;
                    Thread.Sleep(10);
                }
            }
        }

        public UInt64 Connect(IPEndPoint linktoEndPoint)
        {
            var eventArgs = GetFreeEventArgs();
            LinkInfo link = new LinkInfo();
            eventArgs.UserToken = link;
            link.type = LinkType.ConnectedLink;
            link.Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            link.recvArgs = GetFreeEventArgs();
            link.sendArgs = GetFreeEventArgs();
            eventArgs.RemoteEndPoint = linktoEndPoint;
            link.Socket.ConnectAsync(eventArgs);
            this.links[link.Handle] = link;
            return link.Handle;
        }

        public void Send(ulong linkid, byte[] data)
        {
            var link = this.links[linkid];
            link.Socket.SendAsync(link.sendArgs);
        }

        public void CloseLink(ulong linkid)
        {
            this.links[linkid].Socket.Shutdown(SocketShutdown.Both);
        }
    }
}
