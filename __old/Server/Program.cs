using System;
using Network;
using System.Net;
using System.Threading;
using System.Text;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {

            Seez server = new Seez("0.0.0.0", 6666);
            server.OnAccept += (Link link) =>
            {
                IPEndPoint endPoint = link.sock.RemoteEndPoint as IPEndPoint;

                //Console.WriteLine("Accept: {0},{1}", endPoint.Address.ToString(), endPoint.Port);
                link.OnRecv += (byte[] data) =>
                {
                    string sendText = String.Format("server time: {0}", DateTime.Now.Ticks);

                    byte[] datas = Encoding.UTF8.GetBytes(sendText);

                    byte[] bytes = new byte[1024];

                    Buffer.BlockCopy(datas, 0, bytes, 0, datas.Length);
                    link.Send(bytes);
                };

                link.OnClose += (Link lk) =>
                {
                    IPEndPoint ep = lk.sock.RemoteEndPoint as IPEndPoint;
                    Console.WriteLine("Close:{0},{1}", ep.Address.ToString(), endPoint.Port);
                };
            };
            server.Start();
            while (true)
            {

                Console.WriteLine("当前并发数:{0},当前流量  接收:{1}/MB ,发送{2}/MB", server.LinkCount, ((float)Link.recvByte) / 1024 / 1024, ((float)Link.sendByte) / 1024 / 1024);
                Link.recvByte = Link.sendByte = 0;
                Thread.Sleep(1000);
            }
        }
    }
}
