using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace MsgDefine
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MessageIdAttribute : Attribute
    {
        public int Id { get; private set; }

        public MessageIdAttribute(int id)
        {
            Id = id;
        }
    }

    public interface IDataPackage
    {
        int Id { get; set; }
    }

    [MessagePackObject]
    public class MessagePackage
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }
}