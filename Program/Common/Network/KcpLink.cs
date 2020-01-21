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
    public class KcpLink : IReliableDataLink, IKcpLink
    {
        public const int MTU = 1400;

        /// <summary>
        /// 必须保证回调顺序与远端发送顺序一致
        /// </summary>
        public event Action<IMemoryOwner<byte>, int, IReliableDataLink> OnRecvKcpPackage;

        public uint Conv { get; private set; }
        public uint Rtt => _kcp.rx_rtt;

        private Kcp _kcp;
        private CancellationTokenSource _tokenSource;

        private BufferBlock<DataPackage> _recvFromRemote;
        private BufferBlock<DataPackage> _recvFromLocal;

        public void Run(uint conv, IKcpCallback kcpCallback)
        {
            _recvFromRemote = new BufferBlock<DataPackage>();
            _recvFromLocal = new BufferBlock<DataPackage>();

            Conv = conv;

            _kcp = new Kcp(conv, kcpCallback, KcpRentable.Instacne);
            _kcp.NoDelay(1, 10, 2, 1); // 极速模式
            _kcp.WndSize(128, 128);
            _kcp.SetMtu(MTU);

            _tokenSource = new CancellationTokenSource();

            //  这样写的目的，在服务器上，压解缩、加解密可以利用多核（多个KCP连接之间的压解缩、加解密是可以并行的）
            //  update
            Task.Factory.StartNew(async () => {
                //  检测并Flush数据到远端
                while (!_tokenSource.IsCancellationRequested) {
                    try {
                        _kcp.Update(DateTime.UtcNow);
                        await _kcp.CheckAwait(DateTime.UtcNow, _tokenSource.Token);
                    }
                    catch (TaskCanceledException) {
                        break;
                    }
                    catch (ObjectDisposedException e) {
                        //  一般情况下，OutPut时使用Socket发送数据时，而Socket被关闭时会进入此分支；暂时这么处理
                        if (e.ObjectName == "System.Net.Sockets.Socket")
                            break;
                        Debug.LogErrorFormat("Kcp.updateTask: \r\n {0}", e);
                    }
                }
            }, _tokenSource.Token);

            //  input from lower level
            Task.Factory.StartNew(async () => {
                //  收到 Ack 帧，需要移除 snd_buf；收到数据帧，则需要 Flush Ack
                while (!_tokenSource.IsCancellationRequested) {
                    DataPackage package = null;
                    try {
                        package = await _recvFromRemote.ReceiveAsync(_tokenSource.Token);
                        var result = _kcp.Input(package.MemoryOwner.Memory.Span.Slice(package.Offset, package.Lenght));
                        while (result == 0) {
                            var (memoryOwner, avalidLength) = _kcp.TryRecv();
                            if (memoryOwner == null)
                                break;
                            //  在此解压、解密
                            OnRecvKcpPackage?.Invoke(memoryOwner, avalidLength, this);
                        };
                    }
                    catch (TaskCanceledException) {
                        _recvFromRemote.Complete();
                        break;
                    }
                    catch (Exception e) {
                        Debug.LogErrorFormat("Kcp.inputTask: \r\n {0}", e);
                    }
                    finally {
                        if (package != null) {
                            package.MemoryOwner.Dispose();
                            DataPackage.Pool.Return(package);
                        }
                    }
                }
            }, _tokenSource.Token);

            //  send by user
            Task.Factory.StartNew(async () => {
                //  有可能需要发送帧，需要Flush Snd_Queue
                while (!_tokenSource.IsCancellationRequested) {
                    DataPackage package = null;
                    try {
                        package = await _recvFromLocal.ReceiveAsync(_tokenSource.Token);
                        //  在此压缩、加密         
                        _kcp.Send(package.MemoryOwner.Memory.Span.Slice(0, package.Lenght));
                    }
                    catch (TaskCanceledException) {
                        _recvFromLocal.Complete();
                        break;
                    }
                    catch (Exception e) {
                        Debug.LogErrorFormat("Kcp.sendTask: \r\n {0}", e);
                    }
                    finally {
                        if (package != null) {
                            package.MemoryOwner.Dispose();
                            DataPackage.Pool.Return(package);
                        }
                    }
                }
            }, _tokenSource.Token);
        }

        public void Stop(Action onCancel = null)
        {
            OnRecvKcpPackage = null;
            if (onCancel != null)
                _tokenSource.Token.Register(onCancel);
            _tokenSource.Cancel();
        }

        public Task SendToRemoteAsync(IMemoryOwner<byte> memoryOwner, int offset, int len)
        {
            var package = DataPackage.Pool.Get();
            package.Offset = offset;
            package.Lenght = len;
            package.MemoryOwner = memoryOwner;
            return _recvFromLocal.SendAsync(package);
        }

        public Task SendToRemoteAsync((IMemoryOwner<byte> memoryOwner, int len) msg)
        {
            return SendToRemoteAsync(msg.memoryOwner, 0, msg.len);
        }

        public Task RecvFromRemoteAsync(IMemoryOwner<byte> memoryOwner, int offset, int len)
        {
            var package = DataPackage.Pool.Get();
            package.Offset = offset;
            package.Lenght = len;
            package.MemoryOwner = memoryOwner;
            return _recvFromRemote.SendAsync(package);
        }

        public Task RecvFromRemoteAsync(ReadOnlyMemory<byte> memory)
        {
            var len = memory.Length;
            var memoryOwner = MemoryPool<byte>.Shared.Rent(len);
            memory.CopyTo(memoryOwner.Memory);
            return RecvFromRemoteAsync(memoryOwner, 0, len);
        }

    }
}
