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
        static void Listen()
        {
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
            peer1.OnRecv += (id, msg) =>
             {
                 Console.WriteLine("peer1::OnRecv");
                 peer1.Send(id, new byte[] { 05, 00, 3, 1, 2, 3, 4 });

             };
            peer1.Listen(endpoint);



            var linkip = System.Net.IPAddress.Parse("127.0.0.1");

            peer2.OnConnected += (id) =>
            {
                Console.WriteLine("peer2::OnConnected");

                peer2.Send(id, new byte[] { 05, 00, 1, 1, 2, 3, 4 });

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

                peer1.CloseLink(peer1linkid);

            };
            var linkid = peer2.Connect(new System.Net.IPEndPoint(linkip, 8888));
            peer2linkid = linkid;
        }
    }

}

