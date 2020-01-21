using CommonLib.Serializer;
using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace Server.Serializer
{
    public class MsgPackBitSerializer : ISerializer
    {
        [ThreadStatic]
        private static Stream serializeStearm;

        static MsgPackBitSerializer()
        {
            var options = MessagePackSerializerOptions.Standard
                            .WithCompression(MessagePackCompression.Lz4Block);
            MessagePackSerializer.DefaultOptions = options;
        }

        public MsgType Deserialize<MsgType>(ReadOnlyMemory<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<MsgType>(bytes);
        }

        public (IMemoryOwner<byte> buffer, int len) Package<MsgType>(int msgId, MsgType obj)
        {
            if (serializeStearm == null) {
                serializeStearm = new MemoryStream(ushort.MaxValue);
            }
            serializeStearm.Seek(0, SeekOrigin.Begin);
            MessagePackSerializer.Serialize(serializeStearm, obj);
            serializeStearm.Seek(0, SeekOrigin.Begin);
            int len = (int)serializeStearm.Length + 4;
            var memory = MemoryPool<byte>.Shared.Rent(len);
            BinaryPrimitives.WriteInt32LittleEndian(memory.Memory.Span, msgId);
            serializeStearm.Read(memory.Memory.Span.Slice(4));
            return (memory, len);
        }

        public (int msgId, ReadOnlyMemory<byte> body) Unpack(ReadOnlyMemory<byte> bytes)
        {
            return (BinaryPrimitives.ReadInt32LittleEndian(bytes.Span), bytes.Slice(4));
        }
    }
}
