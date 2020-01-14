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

        public readonly static BufferBlock<NetworkBasePackage> _waitForProcessPackage = new BufferBlock<NetworkBasePackage>();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private interface IHandler
        {
            void ReadMessage(ReadOnlyMemory<byte> data);
            byte[] Package<T>(T data);
        }

        private static class HandlerCache<MsgType>
        {
            private class DefaultHandler : IHandler
            {
                public event Action<MsgType> DataHandler;
                private readonly ISerializer _serializer;

                public DefaultHandler(ISerializer serializer)
                {
                    _serializer = serializer;
                    var type = typeof(MsgType);
                    Debug.LogFormat("Handler: {0} Be Created", type.FullName);
                }

                public void ReadMessage(ReadOnlyMemory<byte> data)
                {
                    if (DataHandler != null)
                    {
                        var msg = _serializer.Deserialize<MsgType>(data);
                        DataHandler(msg);
                    }
                }

                public byte[] Package<T>(T data)
                {
                    return _serializer.Package(_id, data);
                }
            }

            private static readonly DefaultHandler _handler;
            private static readonly int _id;

            static HandlerCache()
            {
                var type = typeof(MsgType);
                if (type.GetCustomAttribute(typeof(MessageIdAttribute), false) is MessageIdAttribute msgId)
                {
                    _id = msgId.Id;
                    _handler = new DefaultHandler(DefaultSerializer);
                    Debug.LogFormat("HandlerCache: Cache New Handler:{0}, Id={1}", type.FullName, _id);
                    _handlers.Add(_id, _handler);
                }
                else
                {
                    Debug.LogWarningFormat("HandlerCache: {0} Is Not Msg !", type.FullName);
                }
            }

            public static byte[] PackageMessage(MsgType message)
            {
                if (_handler == null)
                    return default;
                else
                    return _handler.Package(message);
            }

            public static void RegisterHandler(Action<MsgType> action)
            {
                if (_handler != null)
                {
                    _handler.DataHandler += action;
                }
                else
                {
                    Debug.LogWarningFormat("Can Not Register {0}", typeof(MsgType).FullName);
                }
            }

            public static void UnRegisterHandler(Action<MsgType> action)
            {
                if (_handler != null)
                {
                    _handler.DataHandler -= action;
                }
                else
                {
                    Debug.LogWarningFormat("Can Not UnRegister {0}", typeof(MsgType).FullName);
                }
            }

        }

        private static readonly Dictionary<int, IHandler> _handlers = new Dictionary<int, IHandler>();

        public static void RegisterHandler<T>(Action<T> action)
        {
            HandlerCache<T>.RegisterHandler(action);
        }

        public static void UnRegisterHandler<T>(Action<T> action)
        {
            HandlerCache<T>.UnRegisterHandler(action);
        }

        public static Task ProcessBytePackageAsync(IMemoryOwner<byte> memoryOwner, int size)
        {
            //  Pool Get
            var package = NetworkBasePackage.Pool.Get();
            package.MemoryOwner = memoryOwner;
            package.Size = size;
            return _waitForProcessPackage.SendAsync(package);
        }

        public static Task ProcessBytePackageAsync(ReadOnlyMemory<byte> memory)
        {
            //  Pool Get
            var package = NetworkBasePackage.Pool.Get();
            package.MemoryOwner = MemoryPool<byte>.Shared.Rent(memory.Length);
            memory.CopyTo(package.MemoryOwner.Memory);
            package.Size = memory.Length;
            return _waitForProcessPackage.SendAsync(package);
        }

        public static byte[] PackageMessage<T>(T message)
        {
            return HandlerCache<T>.PackageMessage(message);
        }

        public void Run(TaskScheduler scheduler)
        {
            var task = new Task(async () =>
            {
                while (true)
                {
                    var package = await _waitForProcessPackage.ReceiveAsync();
                    var bytes = package.MemoryOwner.Memory.Slice(0, package.Size);
                    var msgPack = DefaultSerializer.Unpack(bytes);
                    if (_handlers.TryGetValue(msgPack.Id, out var handler))
                    {
                        handler.ReadMessage(msgPack.Data);
                    }
                    else
                    {
                        Debug.LogWarningFormat("HandlerManager: MsgId {0} No Handler To Process");
                    }
                    //  Pool Return
                    NetworkBasePackage.Pool.Return(package);
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