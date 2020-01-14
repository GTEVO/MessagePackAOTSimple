using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets.Kcp;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CommonLib.Network
{
    public class KcpLink
    {
        public const int MTU = 1400;

        private Kcp _kcp;
        private uint _conv;

        private CancellationTokenSource _tokenSource;

        private BufferBlock<NetworkBasePackage> _recvFromRemote;
        private BufferBlock<NetworkBasePackage> _recvFromLocal;

        public uint Conv { get { return _conv; } }

        /// <summary>
        /// 必须保证回调顺序与远端发送顺序一致
        /// </summary>
        public event Action<IMemoryOwner<byte>, int, uint> OnRecvKcpPackage;

        public void Run(uint conv, IKcpCallback kcpCallback)
        {
            _recvFromRemote = new BufferBlock<NetworkBasePackage>();
            _recvFromLocal = new BufferBlock<NetworkBasePackage>();

            _conv = conv;
            _kcp = new Kcp(conv, kcpCallback);

            _kcp = new Kcp(1, kcpCallback);
            _kcp.NoDelay(1, 10, 2, 1); // 极速模式
            _kcp.WndSize(64, 64);
            _kcp.SetMtu(MTU);

            _tokenSource = new CancellationTokenSource();
            _tokenSource.Token.Register(() =>
            {
                _kcp.Dispose();
                _kcp = null;
            });

            Task.Run(async () =>
            {
#pragma warning disable CS4014
                // 这样写的目的，在服务器上，压解缩、加解密可以利用多核
                //  input from lower level
                Task.Factory.StartNew(async () =>
                {
                    do
                    {
                        var package = await _recvFromRemote.ReceiveAsync(_tokenSource.Token);
                        var result = _kcp.Input(package.MemoryOwner.Memory.Span.Slice(0, package.Size));
                        while (result == 0)
                        {
                            var (buffer, avalidLength) = _kcp.TryRecv();
                            if (buffer == null)
                                break;
                            //  在此解压、解密
                            OnRecvKcpPackage?.Invoke(buffer, avalidLength, _conv);
                        };
                    } while (!_tokenSource.IsCancellationRequested);
                }, _tokenSource.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
                //  send by user
                Task.Factory.StartNew(async () =>
                {
                    do
                    {
                        var package = await _recvFromLocal.ReceiveAsync(_tokenSource.Token);
                        //  在此压缩、加密
                        _kcp.Send(package.MemoryOwner.Memory.Span.Slice(0, package.Size));
                        NetworkBasePackage.Pool.Return(package);
                    } while (!_tokenSource.IsCancellationRequested);
                }, _tokenSource.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
#pragma warning restore CS4014
                //  update
                do
                {
                    var now = DateTime.UtcNow;
                    await _kcp.UpdateAsync(now);
                    now = DateTime.UtcNow;
                    var delay = _kcp.Check(now);
                    await Task.Delay(delay, _tokenSource.Token);
                } while (!_tokenSource.IsCancellationRequested);
            });
        }

        public Task SendToRemoteAsync(ReadOnlyMemory<byte> buffer)
        {
            var package = NetworkBasePackage.Pool.Get();
            package.Size = buffer.Length;
            package.MemoryOwner = MemoryPool<byte>.Shared.Rent(buffer.Length);
            buffer.CopyTo(package.MemoryOwner.Memory);
            return _recvFromLocal.SendAsync(package);
        }

        public Task RecvFromRemoteAsync(ReadOnlyMemory<byte> buffer)
        {
            var package = NetworkBasePackage.Pool.Get();
            package.Size = buffer.Length;
            package.MemoryOwner = MemoryPool<byte>.Shared.Rent(buffer.Length);
            buffer.CopyTo(package.MemoryOwner.Memory);
            return _recvFromRemote.SendAsync(package);
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }
    }
}
