using System;
using System.Buffers;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Collections.Concurrent;

namespace CommonLib.Network
{
    public class KcpUdpCallback : IKcpCallback
    {
        readonly Socket _Socket;
        readonly EndPoint _remote;
        readonly ConcurrentDictionary<EndPoint, IKcpLink> _networkLinks;

        [ThreadStatic]
        private static byte[] _sendBuffer;
        [ThreadStatic]
        private static byte[] _cmdBuffer;


        public KcpUdpCallback(Socket socket, EndPoint endPoint)
        {
            _Socket = socket;
            _remote = endPoint;
        }

        public KcpUdpCallback(Socket socket, EndPoint endPoint, ConcurrentDictionary<EndPoint, IKcpLink> networkLinks)
        {
            _Socket = socket;
            _remote = endPoint;
            _networkLinks = networkLinks;
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
            _sendBuffer[0] = NetworkCmd.PUSH;
            //  Debug.LogFormat("Thread[{0}] send kcp segment by udp, size = {2}", Thread.CurrentThread.ManagedThreadId, avalidLength);
            avalidLength += 1;
            //  Debug.LogFormat("send PUSH");
            _Socket.SendTo(_sendBuffer, avalidLength, SocketFlags.None, _remote);
            buffer.Dispose();
        }

        public void LostLink(Kcp kcp)
        {
            if (_networkLinks == null)
                return;
            if (_cmdBuffer == null) {
                _cmdBuffer = new byte[1] { NetworkCmd.FIN };
            }
            _Socket.SendTo(_cmdBuffer, 1, SocketFlags.None, _remote);
            if (_networkLinks.TryRemove(_remote, out var link)) {
                link.Stop();
            }
        }
    }
}

