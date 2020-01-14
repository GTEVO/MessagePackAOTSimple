using MsgDefine;
using System;

namespace CommonLib.Serializer
{
    public interface ISerializer
    {
        byte[] Serialize<MsgType>(MsgType obj);
        MsgType Deserialize<MsgType>(ReadOnlyMemory<byte> bytes);

        byte[] Package<MsgType>(int msgId, MsgType obj);
        MessagePackage Unpack(ReadOnlyMemory<byte> bytes);
    }
}
