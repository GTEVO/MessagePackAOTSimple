using System;
using System.Collections.Generic;
using System.Reflection;
using MsgDefine;
using CommonLib.Serializer;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CommonLib.Network;

namespace CommonLib
{
    public class MessageProcessor
    {
        public static ISerializer DefaultSerializer { get; set; }

        public readonly BufferBlock<LinkPackage> _waitForProcessPackage = new BufferBlock<LinkPackage>();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        #region Message Handlers

        private interface IHandler
        {
            void ReadMessage(ReadOnlyMemory<byte> data, IReliableDataLink fromLink);
            (IMemoryOwner<byte> buffer, int len) Package<T>(T data);
        }

        private static class HandlerCache<MsgType>
        {
            private class DefaultHandler : IHandler
            {
                public event Action<MsgType, IReliableDataLink> DataHandler;
                private readonly ISerializer _serializer;

                public DefaultHandler(ISerializer serializer)
                {
                    _serializer = serializer;
                    var type = typeof(MsgType);
                    Debug.LogFormat("Handler: {0} Be Created", type.FullName);
                }

                public void ReadMessage(ReadOnlyMemory<byte> data, IReliableDataLink fromLink)
                {
                    if (DataHandler != null) {
                        var msg = _serializer.Deserialize<MsgType>(data);
                        DataHandler(msg, fromLink);
                    }
                }

                public (IMemoryOwner<byte> buffer, int len) Package<T>(T data)
                {
                    return _serializer.Package(_id, data);
                }
            }

            private static readonly DefaultHandler _handler;
            private static readonly int _id;

            static HandlerCache()
            {
                var type = typeof(MsgType);
                if (type.GetCustomAttribute(typeof(MessageIdAttribute), false) is MessageIdAttribute msgId) {
                    _id = msgId.Id;
                    _handler = new DefaultHandler(DefaultSerializer);
                    Debug.LogFormat("HandlerCache: Cache New Handler:{0}, Id={1}", type.FullName, _id);
                    _handlers.Add(_id, _handler);
                }
                else {
                    Debug.LogWarningFormat("HandlerCache: {0} Is Not Msg !", type.FullName);
                }
            }

            public static (IMemoryOwner<byte> buffer, int len) PackageMessage(MsgType message)
            {
                if (_handler == null)
                    return default;
                else
                    return _handler.Package(message);
            }

            public static void RegisterHandler(Action<MsgType, IReliableDataLink> action)
            {
                if (_handler != null) {
                    _handler.DataHandler += action;
                }
                else {
                    Debug.LogWarningFormat("Can Not Register {0}", typeof(MsgType).FullName);
                }
            }

            public static void UnRegisterHandler(Action<MsgType, IReliableDataLink> action)
            {
                if (_handler != null) {
                    _handler.DataHandler -= action;
                }
                else {
                    Debug.LogWarningFormat("Can Not UnRegister {0}", typeof(MsgType).FullName);
                }
            }

        }

        private static readonly Dictionary<int, IHandler> _handlers = new Dictionary<int, IHandler>();

        #endregion

        public static void RegisterHandler<T>(Action<T, IReliableDataLink> action)
        {
            HandlerCache<T>.RegisterHandler(action);
        }

        public static void UnRegisterHandler<T>(Action<T, IReliableDataLink> action)
        {
            HandlerCache<T>.UnRegisterHandler(action);
        }

        public void ProcessBytePackage(IMemoryOwner<byte> memoryOwner, int size, IReliableDataLink fromLink)
        {
            //  Pool Get
            var package = LinkPackage.Pool.Get();
            package.MemoryOwner = memoryOwner;
            package.Lenght = size;
            package.Link = fromLink;
            _waitForProcessPackage.Post(package);
        }

        public Task ProcessBytePackageAsync(IMemoryOwner<byte> memoryOwner, int size, IReliableDataLink fromLink)
        {
            //  Pool Get
            var package = LinkPackage.Pool.Get();
            package.MemoryOwner = memoryOwner;
            package.Lenght = size;
            package.Link = fromLink;
            return _waitForProcessPackage.SendAsync(package);
        }

        public static (IMemoryOwner<byte> buffer, int len) PackageMessage<T>(T message)
        {
            return HandlerCache<T>.PackageMessage(message);
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="scheduler"> 回调上下文 </param>
        public void Run(TaskScheduler scheduler)
        {
            var task = new Task(async () => {
                while (true) {
                    LinkPackage package = null;
                    try {
                        package = await _waitForProcessPackage.ReceiveAsync();
                        var bytes = package.MemoryOwner.Memory.Slice(0, package.Lenght);
                        var (msgId, body) = DefaultSerializer.Unpack(bytes);
                        if (_handlers.TryGetValue(msgId, out var handler)) {
                            handler.ReadMessage(body, package.Link);
                        }
                        else {
                            Debug.LogWarningFormat("HandlerManager: MsgId {0} No Handler To Process", msgId);
                        }
                    }
                    catch (TaskCanceledException) {
                        break;
                    }
                    catch (Exception e) {
                        Debug.LogErrorFormat("HandlerManager: \r\n {0}", e);
                    }
                    finally {
                        //  Pool Return
                        if (package != null) {
                            package.MemoryOwner.Dispose();
                            DataPackage.Pool.Return(package);
                        }
                    }
                }
            }, _cancellationTokenSource.Token);
            task.Start(scheduler);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}