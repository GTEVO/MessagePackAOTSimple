using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets.Kcp;

namespace CommonLib.Network
{
    public class UdpServer
    {
        Socket _socket;
        EndPoint _remoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

        readonly byte[] _buffer = new byte[ushort.MaxValue];
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly BufferBlock<NetworkPackage> _recvQueue = new BufferBlock<NetworkPackage>();
        readonly ConcurrentDictionary<EndPoint, KcpLink> _networkLinks = new ConcurrentDictionary<EndPoint, KcpLink>();

        public void Start()
        {
            // listen
            var listenIp = new IPEndPoint(IPAddress.Any, 8000);
            _socket = new Socket(listenIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(listenIp);
            var recvTask = new Task(async () => {
                Debug.LogFormat("Recv Bytes From Network Task Run At {0} Thread", Thread.CurrentThread.ManagedThreadId);
                while (!_cancellationTokenSource.IsCancellationRequested) {
                    var result = await Task.Factory.FromAsync(BeginRecvFrom, EndRecvFrom, _socket, TaskCreationOptions.AttachedToParent);
                    //  Pool Get
                    var msgpack = NetworkPackage.Pool.Get();
                    msgpack.Remote = result.remote;
                    msgpack.Size = result.len;
                    msgpack.MemoryOwner = MemoryPool<byte>.Shared.Rent(result.len);
                    ((Span<byte>)_buffer).Slice(0, result.len).CopyTo(msgpack.MemoryOwner.Memory.Span);
                    await _recvQueue.SendAsync(msgpack);
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            recvTask.Start();
            // recv
            var cpuNum = Environment.ProcessorCount;
            for (int i = 0; i < cpuNum; i++) {
                Task.Run(async () => {
                    Debug.LogFormat("Process NetPackage Task Run At {0} Thread", Thread.CurrentThread.ManagedThreadId);
                    do {
                        var package = await _recvQueue.ReceiveAsync();
                        if (!_networkLinks.TryGetValue(package.Remote, out var link)) {
                            link = new KcpLink();
                            link.OnRecvKcpPackage += Link_OnRecvKcpPackage;
                            var s = new KcpUdpCallback(_socket, package.Remote);
                            link.Run(1, s);
                            _networkLinks.TryAdd(package.Remote, link);
                        }
                        await link.RecvFromRemoteAsync(package.MemoryOwner.Memory.Slice(0, package.Size));
                        //  Pool Return
                        NetworkBasePackage.Pool.Return(package);
                    } while (true);
                }, _cancellationTokenSource.Token);
            }
        }

        private void Link_OnRecvKcpPackage(IMemoryOwner<byte> memoryOwner, int len, uint conv)
        {
            MessageProcessor.ProcessBytePackageAsync(memoryOwner, len);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void SendBytes(byte[] bytes)
        {
            foreach (var item in _networkLinks) {
                item.Value.SendToRemoteAsync(bytes);
            }
        }

        #region Implement Socket Cross Platform RecvFromAsync
        private IAsyncResult BeginRecvFrom(AsyncCallback callback, object state)
        {
            var result = _socket.BeginReceiveFrom(_buffer, 0, ushort.MaxValue, SocketFlags.None, ref _remoteEP, callback, state);
            return result;
        }

        private RecvResult EndRecvFrom(IAsyncResult result)
        {
            var recvBytes = _socket.EndReceiveFrom(result, ref _remoteEP);
            var _result = new RecvResult {
                len = recvBytes,
                remote = _remoteEP,
            };
            return _result;
        }
        #endregion
    }
}
