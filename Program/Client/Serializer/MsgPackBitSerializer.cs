using CommonLib.Serializer;
using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using MsgDefine.Resolvers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace ClientLib.Serializer
{
    public class MsgPackBitSerializer : ISerializer
    {
        [ThreadStatic]
        private static MemoryStream serializeStearm;

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
            if (serializeStearm.TryGetBuffer(out var buffer)) {
                buffer.AsMemory().CopyTo(memory.Memory.Slice(4));
            }
            else {
                throw new IOException("Can Not Get SerializeStearm Buffer");
            }
            return (memory, len);
        }

        public (int msgId, ReadOnlyMemory<byte> body) Unpack(ReadOnlyMemory<byte> bytes)
        {
            return (BinaryPrimitives.ReadInt32LittleEndian(bytes.Span), bytes.Slice(4));
        }
    }
}
