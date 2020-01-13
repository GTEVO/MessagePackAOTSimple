namespace CommonLib.Serializer
{
    public interface ISerializer
    {
        byte[] Serialize<MsgType>(MsgType obj);
        MsgType Deserialize<MsgType>(byte[] bytes);
    }
}
