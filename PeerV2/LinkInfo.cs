using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace light.asynctcp
{
    enum LinkType
    {
        AcceptedLink,
        ConnectedLink,
    }
    class LinkInfo
    {
        public LinkType type;
        public Socket Socket;
        public UInt64 Handle;
        public DateTime ActiveDateTime;
        public DateTime ConnectDateTime;
        public SocketAsyncEventArgs recvArgs;
        public SocketAsyncEventArgs sendArgs;
    }
}
