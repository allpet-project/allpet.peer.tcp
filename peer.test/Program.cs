using System;

namespace peer.test
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain();

        }

        static allpet.peer.tcp.IPeer peer1;
        static allpet.peer.tcp.IPeer peer2;

        static async void AsyncMain()
        {
            Console.WriteLine("allpet.peer.tcp.test v0.0001");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "initv1")
                {
                    InitV1();
                }
                else if (line == "initv2")
                {
                    InitV2();
                }
                else if (line == "listen")
                {
                    Listen();
                }
                else if (line == "v2")
                {
                    InitV2();
                    Listen();
                }
                else if (line == "lv2")
                {
                    InitV2();
                    Listen10K();
                }
            }
        }
        static void InitV1()
        {
            peer1 = allpet.peer.tcp.PeerV1.CreatePeer();
            peer2 = allpet.peer.tcp.PeerV1.CreatePeer();
            peer1.Start(new allpet.peer.tcp.PeerOption() { maxSleepTimer = 10000 });
            peer2.Start(new allpet.peer.tcp.PeerOption() { maxSleepTimer = 10000 });
            Console.WriteLine("init v1.");
        }
        static void InitV2()
        {
            peer1 = allpet.peer.tcp.PeerV2.CreatePeer();
            peer2 = allpet.peer.tcp.PeerV2.CreatePeer();
            peer1.Start(new allpet.peer.tcp.PeerOption() { maxSleepTimer = 10000 });
            peer2.Start(new allpet.peer.tcp.PeerOption() { maxSleepTimer = 10000 });
            Console.WriteLine("init v2.");
            Console.WriteLine("init peer1=" + peer1.ID);
            Console.WriteLine("init peer2=" + peer2.ID);
        }
        //测试一个链接
        static void Listen()
        {
            DateTime time = DateTime.Now;
            var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888);
            ulong peer2linkid = 0;
            ulong peer1linkid = 0;
            peer1.OnAccepted += (id, remote) =>
              {
                  Console.WriteLine("peer1::OnAccepted");
                  peer1linkid = id;
              };
            peer1.OnClosed += (id) =>
              {
                  Console.WriteLine("peer1::OnClosed");
              };
            peer1.OnLinkError += (id, err) =>
            {
                Console.WriteLine("peer1::OnLinkError");
            };
            ulong recvsize = 0;
            peer1.OnRecv += (id, msg) =>
             {
                 recvsize += (ulong)msg.Length;
                 if (recvsize % (1024 * 1024) == 0)
                 {
                     Console.WriteLine("peer1::OnRecv " + recvsize);
                 }
                 //peer1.Send(id, new byte[] { 05, 00, 3, 1, 2, 3, 4 });
                 //peer1.Disconnect(id);
                 if (recvsize / (1024 * 1024) == 100)
                 {
                     var total = (DateTime.Now - time).TotalSeconds;
                     Console.WriteLine("total time=" + total);
                 }
             };
            peer1.Listen(endpoint);



            var linkip = System.Net.IPAddress.Parse("127.0.0.1");

            peer2.OnConnected += (id) =>
            {
                Console.WriteLine("peer2::OnConnected");

                for (var i = 0; i < 10 * 1024; i++)
                {
                    var bytes = new byte[10 * 1024];
                    peer2.Send(id, bytes);
                }

            };
            peer2.OnClosed += (id) =>
            {
                Console.WriteLine("peer2::OnClosed");
            };
            peer2.OnLinkError += (id, err) =>
            {
                Console.WriteLine("peer2::OnLinkError");
            };
            peer2.OnRecv += (id, msg) =>
            {
                Console.WriteLine("peer2::OnRecv");

                //peer1.CloseLink(peer1linkid);

            };
            var linkid = peer2.Connect(new System.Net.IPEndPoint(linkip, 8888));
            peer2linkid = linkid;
        }

        static void Listen10K()
        {
            DateTime time = DateTime.Now;
            var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888);
            int p1acceptcount = 0;
            int p1closecount = 0;
            int p1errorcount = 0;
            int p1recvcount = 0;
            peer1.OnAccepted += (id, remote) =>
            {
                p1acceptcount++;
                if (p1acceptcount % 100 == 0)
                {
                    Console.WriteLine("peer1::OnAccepted " + p1acceptcount);

                }
                if (p1acceptcount > 1000)
                {
                    peer1.Disconnect(id);
                }
            };
            peer1.OnClosed += (id) =>
            {
                p1closecount++;
                if (p1closecount % 100 == 0)
                {
                    Console.WriteLine("peer1::OnClosed " + p1closecount);

                }
            };
            peer1.OnLinkError += (id, err) =>
            {
                p1errorcount++;
                if (p1errorcount % 100 == 0)
                {
                    Console.WriteLine("peer1::OnLinkError " + p1acceptcount);
                }
            };
            peer1.OnRecv += (id, msg) =>
            {
                p1recvcount++;
                if (p1recvcount % 100 == 0)
                {
                    Console.WriteLine("peer1::OnRecv " + p1recvcount);
                }
            };
            peer1.Listen(endpoint);




            var linkip = System.Net.IPAddress.Parse("127.0.0.1");
            int p2conncount = 0;
            int p2closecount = 0;
            int p2errorcount = 0;
            peer2.OnConnected += (id) =>
            {
                p2conncount++;
                if (p2conncount % 100 == 0)
                {
                    Console.WriteLine("peer2::OnConnected " + p2conncount);

                }

                var bytes = new byte[12];
                peer2.Send(id, bytes);
            };
            peer2.OnClosed += (id) =>
            {
                p2closecount++;
                if (p2closecount % 100 == 0)
                {
                    Console.WriteLine("peer2::OnClosed " + p2closecount);

                }
            };
            peer2.OnLinkError += (id, err) =>
            {
                p2errorcount++;
                if (p2errorcount % 100 == 0)
                {
                    Console.WriteLine("peer2::OnLinkError " + p2errorcount);

                }
            };
            peer2.OnRecv += (id, msg) =>
            {
                //Console.WriteLine("peer2::OnRecv");

                //peer1.CloseLink(peer1linkid);

            };
            for (var i = 0; i < 10000; i++)
            {
                var linkid = peer2.Connect(new System.Net.IPEndPoint(linkip, 8888));
                if (i % 100 == 0)
                {
                    Console.WriteLine("10k conn." + i);

                }
            }
        }
    }

}

