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
        byte[] _buffer;
        EndPoint _remoteEP;
        CancellationTokenSource _cancellationTokenSource;

        Socket _socket;

        public event Action<IMemoryOwner<byte>, int, uint> OnRecvKcpPackage;

        private KcpLink _kcpLink;

        private async Task ParseCmd(ReadOnlyMemory<byte> buffer)
        {
            byte cmd = buffer.Span[0];
            switch (cmd) {
                case (byte)NetworkCmd.ConnectTo:
                    _kcpLink = new KcpLink();
                    uint conv = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span.Slice(1));
                    _kcpLink.Run(conv, new KcpUdpCallback(_socket, _remoteEP));
                    _kcpLink.OnRecvKcpPackage += KcpLink_OnRecvKcpPackage;
                    break;
                case (byte)NetworkCmd.DependableTransform:
                    await _kcpLink.RecvFromRemoteAsync(buffer.Slice(1));
                    break;
                case (byte)NetworkCmd.KeepAlive:
                    //  TODO
                    break;
                case (byte)NetworkCmd.DisConnect:
                    _kcpLink.OnRecvKcpPackage -= KcpLink_OnRecvKcpPackage;
                    _kcpLink.Stop();
                    break;
            }
        }

        public void Run(IPAddress ip, int port)
        {
            _buffer = new byte[ushort.MaxValue];

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() => {
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
                _kcpLink.OnRecvKcpPackage -= KcpLink_OnRecvKcpPackage;
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
            var cmd = new byte[1400];
            cmd[0] = (byte)NetworkCmd.ConnectTo;
            // 请求参数，例如IdToken放在 cmd[0]后面
            _socket.Send(cmd, SocketFlags.None);
        }

        private void KcpLink_OnRecvKcpPackage(IMemoryOwner<byte> arg1, int arg2, uint arg3)
        {
            OnRecvKcpPackage?.Invoke(arg1, arg2, arg3);
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
