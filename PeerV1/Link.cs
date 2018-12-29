using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace allpet.peer.tcp
{

    internal class PackProcess
    {
        readonly int PACKET_HEADSIZE = 2;

        readonly int MAX_PACKET_SIZE = 65537;

        protected List<byte> packetList = new List<byte>();
        public Action<byte[]> OnRead;
        public void InputData(byte[] data)
        {
            packetList.AddRange(data);

            int offset = 0;

            UInt16 pkgSize = 0;

            while (packetList.Count > PACKET_HEADSIZE)
            {
                pkgSize = (UInt16)(BitConverter.ToUInt16(new byte[] { packetList[0], packetList[1] }, 0));

                if (pkgSize >= MAX_PACKET_SIZE || pkgSize == PACKET_HEADSIZE)
                {
                    Console.WriteLine("size error {0}", pkgSize);
                    return;
                }


                if (pkgSize + offset > packetList.Count)
                    return;

                byte[] bytes = packetList.Skip(2).ToArray(); //new byte[pkgSize];

                //Buffer.BlockCopy(packetList.ToArray(), 2, bytes, 0, pkgSize);

                packetList.RemoveRange(offset, pkgSize);
                if (OnRead != null)
                    OnRead(bytes);
            }
        }
    }

    internal class Link
    {

        readonly int buffersize = 65535;

        public static int recvByte = 0;

        public static int sendByte = 0;


        public Socket sock;

        public event Action<Link> OnClose;
        public event Action<byte[]> OnRecv;
        public SocketAsyncEventArgs recv_Args;

        private PackProcess process = new PackProcess();

        public void Send(byte[] data)
        {
            byte[] bytes = new byte[data.Length + 2];

            Buffer.BlockCopy(BitConverter.GetBytes(bytes.Length), 0, bytes, 0, 2);

            Buffer.BlockCopy(data, 0, bytes, 2, data.Length);

            try
            {
                sock.Send(bytes);
                Link.sendByte += bytes.Length;
            }
            catch (Exception e)
            {
                if (OnClose != null)
                    OnClose(this);
            }
        }

        public bool Connect(string address, int port)
        {
            try
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Parse(address), port);
                sock = new Socket(remote.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(IPAddress.Parse(address), port);

                InitAccept();
                return true;
            }
            catch (Exception e)
            {

                return false;
            }

        }
        public void InitAccept()
        {
            recv_Args = new SocketAsyncEventArgs();
            recv_Args.SetBuffer(new byte[buffersize], 0, buffersize);
            recv_Args.Completed += recv_Args_Completed;
            process.OnRead = (byte[] bytes) =>
            {
                if (this.OnRecv != null)
                    this.OnRecv(bytes);
            };
            RecvProcess();
        }

        public void RecvProcess()
        {
            try
            {
                if (!sock.ReceiveAsync(recv_Args))
                {
                    recv_Args_Completed(sock, recv_Args);
                }
            }
            catch (ObjectDisposedException)
            {
                OnClose(this);
            }
        }
        private void recv_Args_Completed(object sender, SocketAsyncEventArgs e)
        {
            int size = e.BytesTransferred;

            if (size > 0)
            {
                Link.recvByte += size;

                process.InputData(e.Buffer.Take(size).ToArray());
                RecvProcess();
            }
            else
            {
                if (OnClose != null)
                    OnClose(this);

            }
        }


    }
}
