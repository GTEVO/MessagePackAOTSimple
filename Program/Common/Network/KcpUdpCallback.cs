using System;
using System.Buffers;
using System.Buffers.Binary;
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
            byte[] array;
            if (buffer.Memory.Length > avalidLength)
            {
                array = buffer.Memory.ToArray();
                Buffer.BlockCopy(array, 0, array, 1, avalidLength);
                ++avalidLength;
            }
            else
            {
                array = new byte[++avalidLength];
                Buffer.BlockCopy(array, 0, buffer.Memory.ToArray(), 1, avalidLength);
            }
            array[0] = (byte)NetworkCmd.DependableTransform;
            Debug.LogFormat("send udp bytes = {0}", avalidLength);
            _Socket.SendTo(array, avalidLength, SocketFlags.None, _IpendPoint);
            buffer.Dispose();
        }
    }
}

