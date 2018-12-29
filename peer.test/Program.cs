using System;

namespace peer.test
{
    class Program
    {
        static void Main(string[] args)
        {
            var peer1 = allpet.peer.tcp.PeerV1.CreatePeer();

            var peer2 = allpet.peer.tcp.PeerV1.CreatePeer();

            //peer1.Listen 
            //peer2.Connect 2 Peer1

            Console.WriteLine("Hello World!");
        }
    }
}
