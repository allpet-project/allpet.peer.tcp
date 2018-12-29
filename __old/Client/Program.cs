using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Network;


namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {

            StartClients();
        }

        private static void Link_OnRecv(byte[] bytes)
        {
            //Console.WriteLine("msg:{0},len:{1}", Encoding.UTF8.GetString(bytes bytes.Length);
        }


        

        public static void StartClients(int count = 10000)
        {

            List<Link> links = new List<Link>();
            for (int i = 0; i < count; ++i)
            {
                Link link = new Link();
                link.OnRecv += (byte[] bytes) =>
                {
                    Console.WriteLine("收到数据:{0}", bytes.Length);
                };
                link.OnRecv += Link_OnRecv;
                if (link.Connect("127.0.0.1", 6666))
                {
                    Console.WriteLine("连接服务器成功");
                    links.Add(link);
                }
                else
                {
                    Console.WriteLine("连接失败");
                }
            }


            while (true)
            {

                foreach (var item in links)
                {

                    string sendText = String.Format("hello world: {0}", DateTime.Now.Ticks);

                    byte[] datas = Encoding.UTF8.GetBytes(sendText);

                    byte[] bytes = new byte[1024];

                    Buffer.BlockCopy(datas, 0, bytes, 0, datas.Length);

                    //Console.WriteLine("send :{0}", sendText);
                    item.Send(bytes);
                }
                Thread.Sleep(1000);
            }

        }
    }
}
