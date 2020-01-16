using System;
using System.Buffers;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;

namespace CommonLib.Network
{
    public class KcpUdpCallback : IKcpCallback
    {
        readonly Socket _Socket;
        readonly EndPoint _IpendPoint;

        [ThreadStatic]
        private static byte[] _sendBuffer;

        public KcpUdpCallback(Socket socket, EndPoint endPoint)
        {
            _Socket = socket;
            _IpendPoint = endPoint;
        }

        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            if (_sendBuffer == null) {
                _sendBuffer = new byte[ushort.MaxValue];
            }
            if (avalidLength >= ushort.MaxValue) {
                //  正常情况下，KCP不会输出这么大的包
                throw new ArgumentOutOfRangeException("avalidLength", avalidLength, "Too Long");
            }
            var dst = new Span<byte>(_sendBuffer, 1, avalidLength);
            buffer.Memory.Span.Slice(0, avalidLength).CopyTo(dst);
            _sendBuffer[0] = (byte)NetworkCmd.DependableTransform;
            //  Debug.LogFormat("Thread[{0}] send kcp segment by udp, size = {2}", Thread.CurrentThread.ManagedThreadId, avalidLength);
            avalidLength += 1;
            _Socket.SendTo(_sendBuffer, avalidLength, SocketFlags.None, _IpendPoint);
            buffer.Dispose();
        }
    }
}

