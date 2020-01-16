using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using System.Collections.Concurrent;
using System.Buffers.Binary;

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

        readonly MessageProcessor _messageProcessor;

        public UdpServer(MessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
        }

        private async Task ParseCmd(NetworkPackage package)
        {
            ReadOnlyMemory<byte> buffer = package.MemoryOwner.Memory.Slice(0, package.Size);
            byte cmd = buffer.Span[0];
            switch (cmd) {
                case (byte)NetworkCmd.ConnectTo: {
                        //  TODO 验证IdToken，返回 对应的 conv
                        uint conv = 12306;
                        if (_networkLinks.TryRemove(package.Remote, out var link)) {
                            //  移除旧连接
                            link.OnRecvKcpPackage -= Link_OnRecvKcpPackage;
                            link.Stop();
                        }
                        //  建立连接
                        link = new KcpLink();
                        link.OnRecvKcpPackage += Link_OnRecvKcpPackage;
                        var s = new KcpUdpCallback(_socket, package.Remote);
                        link.Run(conv, s);
                        _networkLinks.TryAdd(package.Remote, link);

                        var sendBytes = new byte[5];
                        sendBytes[0] = (byte)NetworkCmd.ConnectTo;
                        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(sendBytes, 1, 4), conv);
                        _socket.SendTo(sendBytes, SocketFlags.None, package.Remote);
                    }
                    break;
                case (byte)NetworkCmd.DependableTransform: {
                        if (_networkLinks.TryGetValue(package.Remote, out var link)) {
                            await link.RecvFromRemoteAsync(buffer.Slice(1));
                        }
                        else {
                            //  这是一个无效连接
                        }
                    }
                    break;
                case (byte)NetworkCmd.KeepAlive:
                    //  TODO
                    break;
                case (byte)NetworkCmd.DisConnect: {
                        if (_networkLinks.TryRemove(package.Remote, out var link)) {
                            link.Stop();
                        }
                    }
                    break;
            }


            //  Pool Return
            NetworkBasePackage.Pool.Return(package);

        }


        public void Start(IPAddress ip, int port)
        {
            //  listen
            var listenIp = new IPEndPoint(ip, port);
            _socket = new Socket(listenIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(listenIp);
            // recv
            var cpuNum = Environment.ProcessorCount;
            for (int i = 0; i < cpuNum; i++) {
                var task = new Task(async () => {
                    Debug.LogFormat("ParseCmd Task Run At Thread [{0}]", Thread.CurrentThread.ManagedThreadId);
                    do {
                        var package = await _recvQueue.ReceiveAsync();
                        await ParseCmd(package);
                    } while (!_cancellationTokenSource.IsCancellationRequested);
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
                task.Start();
            }
            //  acept
            var recvTask = new Task(async () => {
                Debug.LogFormat("Recv Bytes From Network Task Run At Thread[{0}]", Thread.CurrentThread.ManagedThreadId);
                while (!_cancellationTokenSource.IsCancellationRequested) {
                    var result = await Task.Factory.FromAsync(BeginRecvFrom, EndRecvFrom, _socket, TaskCreationOptions.None);
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

        private void Link_OnRecvKcpPackage(IMemoryOwner<byte> memoryOwner, int len, uint conv)
        {
            _messageProcessor.ProcessBytePackageAsync(memoryOwner, len);
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
