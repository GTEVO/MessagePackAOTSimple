﻿using System;

namespace MessageDefine
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MsgIdAttribute : Attribute
    {
        public int Id { get; private set; }

        public MsgIdAttribute(int id)
        {
            Id = id;
        }
    }

    public interface IDataPackage
    {
        int Id { get; set; }
    }

    public class MessagePackage
    {
        public int Id { get; set; }

        public byte[] Data { get; set; }
    }

}
