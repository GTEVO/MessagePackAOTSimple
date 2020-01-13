using System;
using System.Collections.Generic;
using System.Reflection;
using MsgDefine;
using CommonLib.Serializer;
using System.Net;

namespace CommonLib
{
    public static class MessageProcessor
    {
        public static ISerializer DefaultSerializer { get; set; }

        private interface IHandler
        {
            void Unpack(byte[] data, EndPoint remote);
            MessagePackage Package<T>(T data);
        }

        private static class HandlerCache<MsgType>
        {
            private class DefaultHandler : IHandler
            {
                public event Action<MsgType, EndPoint> DataHandler;
                private readonly ISerializer _serializer;

                public DefaultHandler(ISerializer serializer)
                {
                    _serializer = serializer;
                    var type = typeof(MsgType);
                    Debug.LogFormat("Handler: {0} Be Created", type.FullName);
                }

                public void Unpack(byte[] data, EndPoint remote)
                {
                    if (DataHandler != null) {
                        var msg = _serializer.Deserialize<MsgType>(data);
                        DataHandler(msg, remote);
                    }
                }

                public MessagePackage Package<T>(T data)
                {
                    return new MessagePackage {
                        Id = _id,
                        Data = _serializer.Serialize<T>(data)
                    };
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

            public static void RegisterHandler(Action<MsgType, EndPoint> action)
            {
                if (_handler != null) {
                    _handler.DataHandler += action;
                }
                else {
                    Debug.LogWarningFormat("Can Not Register {0}", typeof(MsgType).FullName);
                }
            }

            public static void UnRegisterHandler(Action<MsgType, EndPoint> action)
            {
                if (_handler != null) {
                    _handler.DataHandler -= action;
                }
                else {
                    Debug.LogWarningFormat("Can Not UnRegister {0}", typeof(MsgType).FullName);
                }
            }

            public static MessagePackage PackageMessage(MsgType message)
            {
                if (_handler == null)
                    return default;
                else
                    return _handler.Package<MsgType>(message);
            }
        }

        private static readonly Dictionary<int, IHandler> _handlers = new Dictionary<int, IHandler>();

        public static void RegisterHandler<T>(Action<T, EndPoint> action)
        {
            HandlerCache<T>.RegisterHandler(action);
        }

        public static void UnRegisterHandler<T>(Action<T, EndPoint> action)
        {
            HandlerCache<T>.UnRegisterHandler(action);
        }

        /*******************************************************/
        public static void ProcessMsgPack(MessagePackage msg, EndPoint remote)
        {
            if (_handlers.TryGetValue(msg.Id, out var handler)) {
                handler.Unpack(msg.Data, remote);
            }
            else {
                Debug.LogWarningFormat("HandlerManager: MsgId {0} No Handler To Process", msg.Id);
            }
        }

        public static MessagePackage PackageMessage<T>(T message)
        {
            return HandlerCache<T>.PackageMessage(message);
        }
    }
}