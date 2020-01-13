using CommonLib.Serializer;
using MessagePack;
using MessagePack.Resolvers;
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
        byte[] ISerializer.Serialize<MsgType>(MsgType obj)
        {
            var bytes = MessagePackSerializer.Serialize<MsgType>(obj);
            //  可在此做压缩、加密等工作
            return bytes;
        }

        MsgType ISerializer.Deserialize<MsgType>(ReadOnlyMemory<byte> bytes)
        {
            //  可在此做解缩、解密等工作
            return MessagePackSerializer.Deserialize<MsgType>(bytes);
        }
    }
}
