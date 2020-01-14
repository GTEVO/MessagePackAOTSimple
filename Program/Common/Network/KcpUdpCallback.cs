using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;

namespace CommonLib.Network
{
    public class KcpUdpCallback : IKcpCallback
    {
        readonly Socket _Socket;
        readonly EndPoint _IpendPoint;

        public KcpUdpCallback(Socket socket, EndPoint endPoint)
        {
            _Socket = socket;
            _IpendPoint = endPoint;
        }

        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            Debug.LogFormat("send udp bytes = {0}", avalidLength);
            _Socket.SendTo(buffer.Memory.Span.ToArray(), avalidLength, SocketFlags.None, _IpendPoint);
            buffer.Dispose();
        }
    }
}

