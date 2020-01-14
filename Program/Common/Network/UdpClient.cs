using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Buffers;


namespace CommonLib.Network
{
    public class UdpClient
    {
        byte[] _buffer;
        EndPoint _remoteEP;
        CancellationTokenSource _cancellationTokenSource;

        Socket _socket;

        public KcpLink NetworkLink { get; private set; }

        public void Run()
        {
            _buffer = new byte[ushort.MaxValue];

            NetworkLink = new KcpLink();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() => {
                _socket.Close();
            });

            _remoteEP = new IPEndPoint(IPAddress.Parse("192.168.0.116"), 8000);
            _socket = new Socket(_remoteEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Connect(_remoteEP);

            NetworkLink.Run(1, new KcpUdpCallback(_socket, _remoteEP));

            // listen
            var recvTask = new Task(async () => {
                Debug.LogFormat("Recv Bytes From Network Task Run At {0} Thread", Thread.CurrentThread.ManagedThreadId);
                while (!_cancellationTokenSource.IsCancellationRequested) {
                    var result = await Task.Factory.FromAsync(BeginRecvFrom, EndRecvFrom
                        , _socket, TaskCreationOptions.AttachedToParent);
                    await NetworkLink.RecvFromRemoteAsync(new ReadOnlyMemory<byte>(_buffer, 0, result.len));
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            recvTask.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            NetworkLink.Stop();
        }

        public void SendMessage<T>(T msg)
        {
            var bytes = MessageProcessor.PackageMessage(msg);
            NetworkLink.SendToRemoteAsync(bytes);
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
