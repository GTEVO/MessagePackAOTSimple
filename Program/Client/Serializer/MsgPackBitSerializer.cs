using CommonLib.Serializer;
using MessagePack;
using MsgDefine;
using System;

namespace ClientLib.Serializer
{
    public class MsgPackBitSerializer : ISerializer
    {
        public byte[] Serialize<MsgType>(MsgType obj)
        {
            var bytes = MessagePackSerializer.Serialize(obj);
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
