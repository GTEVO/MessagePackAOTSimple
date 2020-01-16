using CommonLib.Serializer;
using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using MsgDefine.Resolvers;
using System;

namespace ClientLib.Serializer
{
    public class MsgPackBitSerializer : ISerializer
    {
        static MsgPackBitSerializer()
        {
            StaticCompositeResolver.Instance.Register(new IFormatterResolver[]
            {
                GeneratedResolver.Instance,
                MessagePack.Unity.UnityResolver.Instance,
                StandardResolver.Instance,
            });
            // Store it for reuse.
            var options = MessagePackSerializerOptions.Standard
                            .WithResolver(StaticCompositeResolver.Instance)
                            .WithCompression(MessagePackCompression.Lz4Block);
            MessagePackSerializer.DefaultOptions = options;
        }

        public byte[] Serialize<MsgType>(MsgType obj)
        {
            var bytes = MessagePackSerializer.Serialize(obj);
            return bytes;
        }

        public MsgType Deserialize<MsgType>(ReadOnlyMemory<byte> bytes)
        {
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
