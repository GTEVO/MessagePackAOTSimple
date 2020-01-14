﻿using CommonLib.Serializer;
using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using System;

namespace Server.Serializer
{
    public class MsgPackBitSerializer : ISerializer
    {
        public MsgPackBitSerializer()
        {
            /*
            var options = MessagePackSerializerOptions.Standard
                            .WithResolver(StaticCompositeResolver.Instance)
                            .WithCompression(MessagePackCompression.Lz4Block);
            MessagePackSerializer.DefaultOptions = options;
            */
        }
        public byte[] Serialize<MsgType>(MsgType obj)
        {
            var bytes = MessagePackSerializer.Serialize<MsgType>(obj);
            //  可在此做压缩、加密等工作
            return bytes;
        }

        public MsgType Deserialize<MsgType>(ReadOnlyMemory<byte> bytes)
        {
            //  可在此做解缩、解密等工作
            return MessagePackSerializer.Deserialize<MsgType>(bytes);
        }

        public byte[] Package<MsgType>(int msgId, MsgType obj)
        {
            var msgpack = new MessagePackage {
                Id = msgId,
                Data = Serialize(obj),
            };
            return MessagePackSerializer.Serialize(msgpack);
        }

        public MessagePackage Unpack(ReadOnlyMemory<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<MessagePackage>(bytes);
        }
    }
}