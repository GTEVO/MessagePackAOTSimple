using MsgDefine;
using System;
using System.Buffers;

namespace CommonLib.Serializer
{
    public interface ISerializer
    {
        MsgType Deserialize<MsgType>(ReadOnlyMemory<byte> bytes);

        (IMemoryOwner<byte> buffer, int len) Package<MsgType>(int msgId, MsgType obj);
        (int msgId, ReadOnlyMemory<byte> body) Unpack(ReadOnlyMemory<byte> bytes);
    }
}
