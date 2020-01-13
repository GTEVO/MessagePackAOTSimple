using CommonLib.Serializer;
using MessagePack;


namespace ClientLib.Serializer
{
    public class MsgPackBitSerializer : ISerializer
    {
        byte[] ISerializer.Serialize<MsgType>(MsgType obj)
        {
            var bytes = MessagePackSerializer.Serialize<MsgType>(obj);
            //  可在此做压缩、加密等工作
            return bytes;
        }

        MsgType ISerializer.Deserialize<MsgType>(byte[] bytes)
        {
            //  可在此做解缩、解密等工作
            return MessagePackSerializer.Deserialize<MsgType>(bytes);
        }
    }
}
