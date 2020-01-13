using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Buffers;

namespace CommonLib.Network
{
    public class UdpServer
    {
        byte[] _buffer;
        EndPoint _remoteEP;
        CancellationTokenSource _cancellationTokenSource;

        Socket _socket;

        BufferBlock<NetworkPack> _recvQueue = new BufferBlock<NetworkPack>();

        public void Start()
        {
            _buffer = new byte[ushort.MaxValue];

            _remoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            _cancellationTokenSource = new CancellationTokenSource();

            var listenIp = new IPEndPoint(IPAddress.Any, 8000);
            _socket = new Socket(listenIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(listenIp);
            var recvTask = new Task(async () => {
                while (!_cancellationTokenSource.IsCancellationRequested) {
                    var result = await Task.Factory.FromAsync(BeginRecvFrom, EndRecvFrom, _socket, TaskCreationOptions.AttachedToParent);
                    var msgpack = NetworkPack.NetworkPackPool.Get();
                    msgpack.remote = result.remote;
                    msgpack.size = result.len;
                    msgpack.bytes = MemoryPool<byte>.Shared.Rent(result.len);
                    ((Span<byte>)_buffer).Slice(0, result.len).CopyTo(msgpack.bytes.Memory.Span);
                    await _recvQueue.SendAsync(msgpack);
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            recvTask.Start();
        }

        public Task<NetworkPack> RecvAsync()
        {
            return _recvQueue.ReceiveAsync();
        }

        public void Send(byte[] data, EndPoint remote)
        {
            _socket.SendTo(data, remote);
        }

        private IAsyncResult BeginRecvFrom(AsyncCallback callback, object state)
        {
            var result = _socket.BeginReceiveFrom(_buffer, 0, ushort.MaxValue, SocketFlags.None, ref _remoteEP, callback, state);
            return result;
        }

        private RecvResult EndRecvFrom(IAsyncResult result)
        {
            var recvBytes = _socket.EndReceiveFrom(result, ref _remoteEP);
            var rr = new RecvResult {
                len = recvBytes,
                remote = _remoteEP,
            };
            return rr;
        }

    }
}
