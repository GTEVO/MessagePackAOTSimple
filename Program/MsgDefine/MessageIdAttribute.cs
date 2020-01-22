using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace MsgDefine
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class MessageIdAttribute : Attribute
    {
        public int Id { get; private set; }

        public MessageIdAttribute(int id)
        {
            Id = id;
        }
    }
}