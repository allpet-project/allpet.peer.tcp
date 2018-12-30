using System;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Net.Sockets;


namespace allpet.peer.tcp
{
    class Network : allpet.peer.tcp.IPeer
    {
        private Socket listener;
        public EndPoint listenEndpoint;
        //private Semaphore semaphoreAcceptedClients;

        event Action<Link> OnAccept;
        public event Action<ulong, IPEndPoint> OnAccepted;
        public event Action<ulong> OnConnected;
        public event Action<ulong, Exception> OnLinkError;
        public event Action<ulong, byte[]> OnRecv;
        public event Action<ulong> OnCloseed;

        private List<Link> linkList = new List<Link>();


        public int LinkCount
        {
            get
            {
                return linkList.Count;
            }
        }
        //初始化peer 系统
        public void Start(PeerOption option)
        {

        }
        //关闭peer系统
        public void Close()
        {
        }

        public void Listen(IPEndPoint endpoint)
        {
            this.listenEndpoint = endpoint;

            this.listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                this.listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            this.listener.Bind(endpoint);

            int backlog = (int)SocketOptionName.MaxConnections - 1;


            this.listener.Listen(backlog);
            Console.WriteLine("listener :{0},{1}", endpoint.Address.ToString(), endpoint.Port);

            StartAccept(null);
        }
        public void StopListen()
        {

        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                // Socket must be cleared since the context object is being reused.
                acceptEventArg.AcceptSocket = null;
            }

            // this.semaphoreAcceptedClients.WaitOne();
            if (!this.listener.AcceptAsync(acceptEventArg))
            {
                this.ProcessAccept(acceptEventArg);
            }
        }
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket sock = e.AcceptSocket;

            if (sock.Connected)
            {
                Link link = new Link();

                link.sock = sock;

                lock (((ICollection)linkList).SyncRoot)
                    linkList.Add(link);



                link.InitAccept();

                link.OnClose += CloseLink;
                if (OnAccept != null)

                    OnAccept(link);
                if (OnAccepted != null)
                    OnAccepted((UInt64)link.sock.Handle.ToInt64(), (IPEndPoint)link.sock.RemoteEndPoint);

                this.StartAccept(e);
            }

        }

        private void CloseLink(Link link)
        {
            lock (((ICollection)linkList).SyncRoot)
                linkList.Remove(link);
        }



        public UInt64 Connect(IPEndPoint linktoEndPoint)
        {
            return 0;

        }

        public void Send(ulong link, byte[] data)
        {
        }

        public void CloseLink(ulong linkid)
        {
            //var link = linkList[linkid];
            //CloseLink(link);
        }

        public void Dispose()
        {
            this.Close();
        }
    }
}
