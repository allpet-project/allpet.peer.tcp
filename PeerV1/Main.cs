using System;
using System.Collections.Generic;
using System.Text;

namespace allpet.peer.tcp 
{
    public class PeerV1
    {
        public static IPeer CreatePeer()
        {
            return new Network();
        }
    }
}
