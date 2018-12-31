using System;
using System.Net.Sockets;

namespace light.asynctcp
{
    partial class ServerModule
    {
        //Semaphore _maxAcceptedClients;//用信号量控制最大连接数
        System.Collections.Concurrent.ConcurrentDictionary<UInt64, LinkInfo> links;

        void InitProcess()
        {
            //int _maxClient = 10000;
            links = new System.Collections.Concurrent.ConcurrentDictionary<UInt64, LinkInfo>();
            //_maxAcceptedClients = new Semaphore(_maxClient, _maxClient);

        }
        /// <summary>  
        /// 监听Socket接受处理  
        /// </summary>  
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>  
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket s = e.AcceptSocket;//和客户端关联的socket  
                if (s != null && s.Connected)
                {
                    try
                    {

                        SocketAsyncEventArgs asyniar = GetFreeEventArgs();
                        var link = new LinkInfo();
                        link.type = LinkType.AcceptedLink;
                        asyniar.UserToken = link;

                        //用户的token操作
                        link.Socket = s;
                        link.Handle = (UInt64)s.Handle.ToInt64();
                        //token.ID = System.Guid.NewGuid().ToString();
                        link.ConnectDateTime = DateTime.Now;
                        link.recvArgs = GetFreeEventArgs();
                        link.sendArgs = GetFreeEventArgs();
                        link.recvArgs.UserToken = link;
                        link.sendArgs.UserToken = link;
                        this.links.TryAdd((UInt64)link.Handle, link);

                        //s.Send(Encoding.UTF8.GetBytes("Your GUID:" + token.ID));

                        Console.WriteLine("client in:" + this.links.Count);

                        this.OnAccepted(link.Handle, link.Socket.RemoteEndPoint as System.Net.IPEndPoint);

                        link.recvArgs.SetBuffer(new byte[1024], 0, 1024);
                        var asyncr = link.Socket.ReceiveAsync(link.recvArgs);
                        if (!asyncr)
                        {
                            ProcessReceice(link.recvArgs, link);
                        }
                    }
                    catch (SocketException ex)
                    {

                        Console.WriteLine(String.Format("接收客户 {0} 数据出错, 异常信息： {1} 。", s.RemoteEndPoint, ex.ToString()));
                        //TODO 异常处理  
                    }
                    //投递下一个接受请求,這裏就直接復用了
                    StartAccept(e);
                }
            }
            else
            {
                throw new Exception("accept error.");
                //ProcessDisConnect(e, e.UserToken as LinkInfo);
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs e, LinkInfo link)
        {
            link.recvArgs = GetFreeEventArgs();
            link.sendArgs = GetFreeEventArgs();
            link.recvArgs.UserToken = link;
            link.sendArgs.UserToken = link;

            this.OnConnected(link.Handle);

            link.recvArgs.SetBuffer(new byte[1024], 0, 1024);
            var asyncr = link.Socket.ReceiveAsync(link.recvArgs);
            if (!asyncr)
            {
                ProcessReceice(link.recvArgs, link);
            }

            //復用一個connect args
            e.UserToken = null;
            this.PushBackEventArgs(e);
        }
        private unsafe void ProcessReceice(SocketAsyncEventArgs e, LinkInfo link)
        {
            var pi = e.ReceiveMessageFromPacketInfo;
            if (e.BytesTransferred == 0)
            {
                //接收0转过去的,这个e不给他回收
                ProcessRecvZero(link);
                return;
            }
            byte[] data = new byte[e.BytesTransferred];

            fixed (byte* a = e.Buffer)
            {
                fixed (byte* dest = data)
                {
                    System.Buffer.MemoryCopy(a, dest, e.BytesTransferred, e.BytesTransferred);
                }
            }
            this.OnRecv(link.Handle, data);

            var asyncr = link.Socket.ReceiveAsync(link.recvArgs);
            if (!asyncr)
            {
                ProcessReceice(e, link);
            }

        }
        private void ProcessSend(SocketAsyncEventArgs e, LinkInfo link)
        {

        }
        private void ProcessDisConnect(SocketAsyncEventArgs e, LinkInfo link)
        {//收到这个是主动断线一方
            try
            {
                link.Socket.Close();
                link.Socket = null;
            }
            catch(Exception err)
            {

            }
            PushBackEventArgs(e);
        }
        /// <summary>
        /// 收到零个字节代表这个连接被断开了
        /// </summary>
        /// <param name="link"></param>
        private void ProcessRecvZero(LinkInfo link)
        {
            this.links.TryRemove(link.Handle, out LinkInfo v);

            this.OnClosed(link.Handle);
        }
    }
}
