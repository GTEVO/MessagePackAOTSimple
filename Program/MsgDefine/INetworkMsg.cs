using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace MsgDefine
{
    public interface INetworkMsg
    {
        int Id { get; set; }
    }

    public abstract class BaseNetworkMsg : INetworkMsg
    {
        [Key(0)]
        public int Id { get; set; }
    }
}