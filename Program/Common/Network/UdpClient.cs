using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using System.Buffers.Binary;

namespace CommonLib.Network
{
    public class UdpClient
    {
        public const int InitalTimeOut = 200;
        public const int TimeOut = 1000 * 30;
        public const float Rate = 1.750f;

        byte[] _buffer;
        EndPoint _remoteEP;
        CancellationTokenSource _cancellationTokenSource;
        CancellationTokenSource _synTaskCancellationTokenSource;

        Socket _socket;

        public event Action<IMemoryOwner<byte>, int, IReliableDataLink> OnRecvKcpPackage;

        private KcpLink _kcpLink;

        public uint ConectionId => _kcpLink == null ? 0 : _kcpLink.Conv;

        public int Rtt => _kcpLink == null ? -1 : _kcpLink.Rtt;

        private async Task ParseCmd(ReadOnlyMemory<byte> buffer)
        {
            byte cmd = buffer.Span[0];
            if ((cmd & NetworkCmd.SYN) != 0) {
                if ((cmd & NetworkCmd.ACK) != 0) {
                    //  Debug.LogFormat("recv SYN +ACK");
                    //  syn ack
                    uint conv = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span.Slice(1));
                    if (_kcpLink == null) {
                        _kcpLink = new KcpLink();
                        _kcpLink.Run(conv, new KcpUdpCallback(_socket, _remoteEP));
                        _kcpLink.OnRecvKcpPackage += KcpLink_OnRecvKcpPackage;
                    }
                    //  发送 ayn ack
                    var sendBytes = new byte[5];
                    sendBytes[0] = NetworkCmd.SYN | NetworkCmd.ACK;
                    BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(sendBytes, 1, 4), conv);
                    //  Debug.LogFormat("send SYN + ACK");
                    _socket.Send(sendBytes, SocketFlags.None);
                    _synTaskCancellationTokenSource.Cancel();
                }
                else {
                    // syn 客户端不存在这种情况
                }
            }
            else if ((cmd & NetworkCmd.PUSH) != 0) {
                //  Debug.LogFormat("recv PUSH");
                await _kcpLink.RecvFromRemoteAsync(buffer.Slice(1));
            }
            else if ((cmd & NetworkCmd.FIN) != 0) {
                //  Debug.LogFormat("recv FIN");
                _kcpLink.Stop();
            }
        }

        public void Run(IPAddress ip, int port)
        {
            _buffer = new byte[ushort.MaxValue];

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() => {
                var sendBytes = new byte[1];
                sendBytes[0] = NetworkCmd.FIN;
                _socket.SendTo(sendBytes, SocketFlags.None, _remoteEP);
                _socket.Close();
            });

            _remoteEP = new IPEndPoint(ip, port);
            _socket = new Socket(_remoteEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.IgnoreRemoteHostClosedException();
            _socket.Connect(_remoteEP);

            //  RecvFromAsync Task
            var recvTask = new Task(async () => {
                Debug.LogFormat("Recv Bytes From Network Task Run At Thread[{0}]", Thread.CurrentThread.ManagedThreadId);
                while (!_cancellationTokenSource.IsCancellationRequested) {
                    var result = await Task.Factory.FromAsync(BeginRecvFrom, EndRecvFrom, _socket, TaskCreationOptions.None);
                    var memory = new ReadOnlyMemory<byte>(_buffer, 0, result.len);
                    await ParseCmd(memory);
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            recvTask.Start();

            ConnectTo();
        }

        public void Stop()
        {
            if (_kcpLink != null) {
                _kcpLink?.Stop(_Stop);
            }
            else {
                _Stop();
            }
        }

        private void _Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void SendMessage<T>(T msg)
        {
            if (_kcpLink == null)
                return;
            var bytes = MessageProcessor.PackageMessage(msg);
            _kcpLink.SendToRemoteAsync(bytes);
        }

        public void ConnectTo()
        {
            //  
            var task = new Task(async () => {
                var cmd = new byte[1400];
                cmd[0] = (byte)NetworkCmd.SYN;
                // 请求参数，例如IdToken放在 cmd[0]后面
                int delay = InitalTimeOut;
                _synTaskCancellationTokenSource = new CancellationTokenSource();
                do {
                    //  发送conv
                    //  Debug.LogFormat("send SYN");
                    _socket.Send(cmd, SocketFlags.None);
                    try {
                        await Task.Delay(delay, _synTaskCancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException) {
                        break;
                    }
                    delay = (int)(delay * Rate);
                } while (!_synTaskCancellationTokenSource.IsCancellationRequested);

            });
            task.Start();
        }

        private void KcpLink_OnRecvKcpPackage(IMemoryOwner<byte> arg1, int arg2, IReliableDataLink link)
        {
            OnRecvKcpPackage?.Invoke(arg1, arg2, link);
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
            var rr = new RecvResult {
                len = recvBytes,
                remote = _remoteEP,
            };
            return rr;
        }
        #endregion
    }
}
