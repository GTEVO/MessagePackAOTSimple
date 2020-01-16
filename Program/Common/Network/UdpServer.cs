using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using System.Collections.Concurrent;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace CommonLib.Network
{
    public class UdpServer
    {
        public const int InitalTimeOut = 200;
        public const int TimeOut = 1000 * 30;
        public const float Rate = 1.750f;

        Socket _socket;
        EndPoint _remoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

        readonly byte[] _buffer = new byte[ushort.MaxValue];
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly BufferBlock<NetworkPackage> _recvQueue = new BufferBlock<NetworkPackage>();
        readonly ConcurrentDictionary<EndPoint, KcpLink> _networkLinks = new ConcurrentDictionary<EndPoint, KcpLink>();

        readonly MessageProcessor _messageProcessor;

        readonly ConcurrentDictionary<EndPoint, CancellationTokenSource> _synAckTaskTokens = new ConcurrentDictionary<EndPoint, CancellationTokenSource>();

        public UdpServer(MessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
            Task.Run(async () =>
            {
                do
                {
                    Debug.LogFormat("clients link count : {0}", _networkLinks.Count);
                    await Task.Delay(1000);
                } while (!_cancellationTokenSource.IsCancellationRequested);
            }, _cancellationTokenSource.Token);
        }

        private async Task ParseCmd(NetworkPackage package)
        {
            ReadOnlyMemory<byte> buffer = package.MemoryOwner.Memory.Slice(0, package.Size);
            byte cmd = buffer.Span[0];

            if ((cmd & NetworkCmd.SYN) != 0)
            {
                if ((cmd & NetworkCmd.ACK) == 0)
                {
                    //  Debug.LogFormat("recv SYN", package.Remote);
                    //  连接请求 syn
                    //  TODO 验证IdToken，返回 对应的 conv
                    uint conv = 1;
                    if (package.Remote is IPEndPoint ip)
                    {
                        //  先用对方的Port作为连接号吧
                        conv = (uint)ip.Port;
                    }

                    var remote = package.Remote;
                    if (!_synAckTaskTokens.ContainsKey(remote))
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        if (_synAckTaskTokens.TryAdd(remote, cancellationTokenSource))
                        {
                            var synAck = new Task(async () =>
                            {
                                var sendBytes = new byte[5];
                                sendBytes[0] = NetworkCmd.SYN | NetworkCmd.ACK;
                                BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(sendBytes, 1, 4), conv);
                                int delay = InitalTimeOut;
                                do
                                {
                                    //  发送conv
                                    //  Debug.LogFormat("send SYN + ACK", package.Remote);
                                    _socket.SendTo(sendBytes, SocketFlags.None, remote);
                                    try
                                    {
                                        await Task.Delay(delay, cancellationTokenSource.Token);
                                    }
                                    catch (TaskCanceledException)
                                    {
                                        break;
                                    }
                                    delay = (int)(delay * Rate);
                                } while (!cancellationTokenSource.IsCancellationRequested);
                                _synAckTaskTokens.TryRemove(remote, out var token);
                            });
                            synAck.Start();
                        }
                    }
                }
                else
                {
                    //  Debug.LogFormat("recv SYN + ACK", package.Remote);
                    var conv = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span.Slice(1));
                    var remote = package.Remote;
                    //  连接确认 syn ack
                    if (_synAckTaskTokens.TryRemove(package.Remote, out var token))
                    {
                        //  移除旧连接
                        if (_networkLinks.TryRemove(package.Remote, out var link))
                        {
                            link.OnRecvKcpPackage -= Link_OnRecvKcpPackage;
                            link.Stop();
                        }
                        //  建立连接
                        link = new KcpLink();
                        link.OnRecvKcpPackage += Link_OnRecvKcpPackage;
                        var kcpOutPut = new KcpUdpCallback(_socket, package.Remote);
                        link.Run(conv, kcpOutPut);
                        _networkLinks.TryAdd(package.Remote, link);
                        token.Cancel();
                    }
                    else
                    {
                        if (_networkLinks.TryGetValue(package.Remote, out var link))
                        {
                            if (link.Conv != conv)
                            {
                                //  连接号已被重置，需要重新请求连接
                                var sendBytes = new byte[1];
                                sendBytes[0] = NetworkCmd.RESET;
                                //  Debug.LogFormat("send RESET", package.Remote);
                                _socket.SendTo(sendBytes, SocketFlags.None, remote);
                            }
                        }
                        else
                        {
                            //  连接已不存在，需要重新请求连接
                            var sendBytes = new byte[1];
                            sendBytes[0] = NetworkCmd.RESET;
                            //  Debug.LogFormat("send RESET", package.Remote);
                            _socket.SendTo(sendBytes, SocketFlags.None, remote);
                        }
                    }
                }
            }
            else if ((cmd & NetworkCmd.PUSH) != 0)
            {
                //  Debug.LogFormat("recv PUSH");
                //  数据
                if (_networkLinks.TryGetValue(package.Remote, out var link))
                {
                    await link.RecvFromRemoteAsync(buffer.Slice(1));
                }
            }
            else if ((cmd & NetworkCmd.FIN) != 0)
            {
                //  Debug.LogFormat("recv FIN");
                //  断开连接
                if (_networkLinks.TryRemove(package.Remote, out var link))
                {
                    link.Stop();
                }
            }

            //  Pool Return
            NetworkBasePackage.Pool.Return(package);
        }


        public void Start(IPAddress ip, int port)
        {
            //  listen
            var listenIp = new IPEndPoint(ip, port);
            _socket = new Socket(listenIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.IgnoreRemoteHostClosedException();
            _socket.Bind(listenIp);
            // recv
            var cpuNum = Environment.ProcessorCount * 1.5;
            for (int i = 0; i < cpuNum; i++)
            {
                var task = new Task(async () =>
                {
                    Debug.LogFormat("ParseCmd Task Run At Thread [{0}]", Thread.CurrentThread.ManagedThreadId);
                    do
                    {
                        var package = await _recvQueue.ReceiveAsync();
                        await ParseCmd(package);
                    } while (!_cancellationTokenSource.IsCancellationRequested);
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
                task.Start();
            }
            //  acept
            var recvTask = new Task(async () =>
            {
                Debug.LogFormat("Recv Bytes From Network Task Run At Thread[{0}]", Thread.CurrentThread.ManagedThreadId);
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
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
            foreach (var item in _networkLinks)
            {
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
            var _result = new RecvResult
            {
                len = recvBytes,
                remote = _remoteEP,
            };
            return _result;
        }
        #endregion
    }
}
