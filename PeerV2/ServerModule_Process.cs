using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace light.asynctcp
{
    public partial class ServerModule
    {
        Semaphore _maxAcceptedClients;//用信号量控制最大连接数
        System.Collections.Concurrent.ConcurrentDictionary<UInt64, LinkInfo> links;

        void InitProcess()
        {
            int _maxClient = 10000;
            links = new System.Collections.Concurrent.ConcurrentDictionary<UInt64, LinkInfo>();
            _maxAcceptedClients = new Semaphore(_maxClient, _maxClient);

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

                        this.links.TryAdd((UInt64)link.Handle, link);

                        //s.Send(Encoding.UTF8.GetBytes("Your GUID:" + token.ID));

                        Console.WriteLine("client in:" + this.links.Count);

                        //if (!s.ReceiveAsync(asyniar))//投递接收请求  
                        //{
                        //    ProcessReceive(asyniar);
                        //}
                    }
                    catch (SocketException ex)
                    {

                        Console.WriteLine(String.Format("接收客户 {0} 数据出错, 异常信息： {1} 。", s.RemoteEndPoint, ex.ToString()));
                        //TODO 异常处理  
                    }
                    //投递下一个接受请求  
                    StartAccept(e);
                }
            }
            else
            {
                CloseClientSocket(e); ;
            }
        }


        public void CloseClientSocket(SocketAsyncEventArgs e)
        {
            LinkInfo token = (LinkInfo)e.UserToken;
            if (token == null)
            {
                e.AcceptSocket.Close();
                _maxAcceptedClients.Release();//释放线程信号量
                return;
            }

            if (e.SocketError == SocketError.OperationAborted || e.SocketError == SocketError.ConnectionAborted)
                return;

            Console.WriteLine(String.Format("客户 {0} 断开连接!", token.Socket.RemoteEndPoint.ToString()));

            Socket s = token.Socket;
            try
            {
                s.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                // Throw if client has closed, so it is not necessary to catch.
                //Log4Debug(ex.StackTrace);
            }
            finally
            {
                s.Close();
                s = null;
            }
            _maxAcceptedClients.Release();//释放线程信号量
            PushBackEventArgs(e);//SocketAsyncEventArg 对象被释放，压入可重用队列。
            links.TryRemove((UInt64)token.Handle, out LinkInfo info);//去除正在连接的用户

        }

    }
}
