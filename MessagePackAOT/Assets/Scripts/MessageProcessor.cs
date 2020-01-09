using MessageDefine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MessagePack;


public sealed class MessageProcessor
{
    private interface IHandler
    {
        void Unpack(byte[] data);
        MessagePackage Package<T>(T data);
    }

    private static class HandlerCache<MsgType>
    {
        private class Handler : IHandler
        {
            public event Action<MsgType> DataHandler;

            public Handler()
            {
                var type = typeof(MsgType);
                Debug.LogFormat("Handler: {0} Be Created", type.FullName);
            }

            public void Unpack(byte[] data)
            {
                if (DataHandler != null) {
                    var msg = MessagePackSerializer.Deserialize<MsgType>(data);
                    DataHandler(msg);
                }
            }

            public MessagePackage Package<T>(T data)
            {
                return new MessagePackage {
                    Id = _id,
                    Data = MessagePackSerializer.Serialize(data)
                };
            }
        }

        private static readonly Handler _handler;
        private static readonly int _id;

        static HandlerCache()
        {
            var type = typeof(MsgType);
            if (type.GetCustomAttribute(typeof(MessageIdAttribute), false) is MessageIdAttribute msgId) {
                _id = msgId.Id;
                _handler = new Handler();
                Debug.LogFormat("HandlerCache: Cache New Handler:{0}, Id={1}", type.FullName, _id);
                _handlers.Add(_id, _handler);
            }
            else {
                Debug.LogWarningFormat("HandlerCache: {0} Is Not Msg !", type.FullName);
            }
        }

        public static void RegisterHandler(Action<MsgType> action)
        {
            if (_handler != null) {
                _handler.DataHandler += action;
            }
            else {
                Debug.LogWarningFormat("Can Not Register {0}", typeof(MsgType).FullName);
            }
        }

        public static void UnRegisterHandler(Action<MsgType> action)
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

    public static void RegisterHandler<T>(Action<T> action)
    {
        HandlerCache<T>.RegisterHandler(action);
    }

    public static void UnRegisterHandler<T>(Action<T> action)
    {
        HandlerCache<T>.UnRegisterHandler(action);
    }

    public static void ProcessMsgPack(MessagePackage msg)
    {
        if (_handlers.TryGetValue(msg.Id, out var handler)) {
            handler.Unpack(msg.Data);
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