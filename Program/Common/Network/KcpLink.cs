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
        private CancellationTokenSource _tokenSource;

        private BufferBlock<NetworkBasePackage> _recvFromRemote;
        private BufferBlock<NetworkBasePackage> _recvFromLocal;

        [ThreadStatic]
        private static TaskScheduler _synchronizationContextTaskScheduler;

        public uint Conv { get; private set; }

        /// <summary>
        /// 必须保证回调顺序与远端发送顺序一致
        /// </summary>
        public event Action<IMemoryOwner<byte>, int, uint> OnRecvKcpPackage;

        public void Run(uint conv, IKcpCallback kcpCallback)
        {
            _recvFromRemote = new BufferBlock<NetworkBasePackage>();
            _recvFromLocal = new BufferBlock<NetworkBasePackage>();

            Conv = conv;
            _kcp = new Kcp(conv, kcpCallback);

            _kcp = new Kcp(1, kcpCallback);
            _kcp.NoDelay(1, 10, 2, 1); // 极速模式
            _kcp.WndSize(128, 128);
            _kcp.SetMtu(MTU);

            _tokenSource = new CancellationTokenSource();

            //  从解析命令的上下文（线程）中切出去，由默认调度器（线程池调度器）启动该任务
            Task.Run(() => {

                //  Debug.LogFormat("Kcp.cnov:{0} Task Run At Thread[{1}]", conv, Thread.CurrentThread.ManagedThreadId);

                if (_synchronizationContextTaskScheduler == null) {
                    if (SynchronizationContext.Current == null) {
                        var synchronizationContext = new SynchronizationContext();
                        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                    }
                    _synchronizationContextTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    Debug.LogFormat("Create SynchronizationContext TaskScheduler For Thread[{1}]", conv, Thread.CurrentThread.ManagedThreadId);
                }

                //  这样写的目的，在服务器上，压解缩、加解密可以利用多核（多个KCP连接之间的压解缩、加解密是可以并行的）
                //  update
                Task updateTask = new Task(async () => {
                    //  Debug.LogFormat("Kcp.cnov:{0} Update Task Run At Thread[{1}]", conv, Thread.CurrentThread.ManagedThreadId);
                    while (!_tokenSource.IsCancellationRequested) {
                        try {
                            var now = DateTime.UtcNow;
                            await _kcp.UpdateAsync(now);
                            now = DateTime.UtcNow;
                            var delay = _kcp.Check(now);
                            await Task.Delay(delay, _tokenSource.Token);
                        }
                        catch (TaskCanceledException) {
                            break;
                        }
                        catch (ObjectDisposedException e) {
                            //  一般情况下，Socket发送数据时，被关闭时会进入此分支；暂时这么处理
                            if (e.ObjectName == "System.Net.Sockets.Socket")
                                break;
                            Debug.LogErrorFormat("Kcp.updateTask: \r\n {0}", e);
                        }
                    }
                });
                updateTask.Start(_synchronizationContextTaskScheduler);

                //  input from lower level
                Task inputTask = new Task(async () => {
                    //  Debug.LogFormat("Kcp.cnov:{0} Input Task Run At Thread[{1}]", conv, Thread.CurrentThread.ManagedThreadId);
                    while (!_tokenSource.IsCancellationRequested) {
                        try {
                            var package = await _recvFromRemote.ReceiveAsync(_tokenSource.Token);
                            var result = _kcp.Input(package.MemoryOwner.Memory.Span.Slice(0, package.Size));
                            while (result == 0) {
                                var (buffer, avalidLength) = _kcp.TryRecv();
                                if (buffer == null)
                                    break;
                                //  在此解压、解密
                                OnRecvKcpPackage?.Invoke(buffer, avalidLength, Conv);
                            };
                        }
                        catch (TaskCanceledException) {
                            break;
                        }
                        catch (Exception e) {
                            Debug.LogErrorFormat("Kcp.inputTask: \r\n {0}", e);
                        }
                    }
                }, _tokenSource.Token);
                inputTask.Start(_synchronizationContextTaskScheduler);

                //  send by user
                Task sendTask = new Task(async () => {
                    //  Debug.LogFormat("Kcp.cnov:{0} Send Task Run At Thread[{1}]", conv, Thread.CurrentThread.ManagedThreadId);
                    while (!_tokenSource.IsCancellationRequested) {
                        try {
                            var package = await _recvFromLocal.ReceiveAsync(_tokenSource.Token);
                            //  在此压缩、加密                       
                            _kcp.Send(package.MemoryOwner.Memory.Span.Slice(0, package.Size));
                            NetworkBasePackage.Pool.Return(package);
                        }
                        catch (TaskCanceledException) {
                            break;
                        }
                        catch (Exception e) {
                            Debug.LogErrorFormat("Kcp.sendTask: \r\n {0}", e);
                        }
                    }
                }, _tokenSource.Token);
                sendTask.Start(_synchronizationContextTaskScheduler);

            }, _tokenSource.Token).ContinueWith(task => {
                //  预测创建同步上下文，获取调度器之前线程发生切换。之后可能会异常，概率应该不大，就不处理了
                Debug.LogErrorFormat("KcpLink.cnov:{0}\r\n[{1}]", conv, task.Exception);
                Stop();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop(Action onCancel = null)
        {
            if (onCancel != null)
                _tokenSource.Token.Register(onCancel);
            _tokenSource.Cancel();
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

    }
}
