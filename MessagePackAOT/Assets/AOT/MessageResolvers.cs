// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
    using System;
    using System.Buffers;
    using MessagePack;

    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

        private GeneratedResolver()
        {
        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class GeneratedResolverGetFormatterHelper
    {
        private static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(4)
            {
                { typeof(global::MessageDefine.MessagePackage), 0 },
                { typeof(global::Msgs.TestMsg1), 1 },
                { typeof(global::Msgs.TestMsg2), 2 },
                { typeof(global::Msgs.Position), 3 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key))
            {
                return null;
            }

            switch (key)
            {
                case 0: return new MessagePack.Formatters.MessageDefine.MessagePackageFormatter();
                case 1: return new MessagePack.Formatters.Msgs.TestMsg1Formatter();
                case 2: return new MessagePack.Formatters.Msgs.TestMsg2Formatter();
                case 3: return new MessagePack.Formatters.Msgs.PositionFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1649 // File name should match first type name




// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.MessageDefine
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class MessagePackageFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::MessageDefine.MessagePackage>
    {


        public void Serialize(ref MessagePackWriter writer, global::MessageDefine.MessagePackage value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            writer.Write(value.Id);
            formatterResolver.GetFormatterWithVerify<byte[]>().Serialize(ref writer, value.Data, options);
        }

        public global::MessageDefine.MessagePackage Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Id__ = default(int);
            var __Data__ = default(byte[]);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Id__ = reader.ReadInt32();
                        break;
                    case 1:
                        __Data__ = formatterResolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::MessageDefine.MessagePackage();
            ____result.Id = __Id__;
            ____result.Data = __Data__;
            return ____result;
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Msgs
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class TestMsg1Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Msgs.TestMsg1>
    {


        public void Serialize(ref MessagePackWriter writer, global::Msgs.TestMsg1 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.Name, options);
        }

        public global::Msgs.TestMsg1 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Name__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Name__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Msgs.TestMsg1();
            ____result.Name = __Name__;
            return ____result;
        }
    }

    public sealed class TestMsg2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Msgs.TestMsg2>
    {


        public void Serialize(ref MessagePackWriter writer, global::Msgs.TestMsg2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.Age);
        }

        public global::Msgs.TestMsg2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Age__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Age__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Msgs.TestMsg2();
            ____result.Age = __Age__;
            return ____result;
        }
    }

    public sealed class PositionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Msgs.Position>
    {


        public void Serialize(ref MessagePackWriter writer, global::Msgs.Position value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Serialize(ref writer, value.Last, options);
            formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Serialize(ref writer, value.Current, options);
        }

        public global::Msgs.Position Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Last__ = default(global::UnityEngine.Vector3);
            var __Current__ = default(global::UnityEngine.Vector3);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Last__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        __Current__ = formatterResolver.GetFormatterWithVerify<global::UnityEngine.Vector3>().Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Msgs.Position();
            ____result.Last = __Last__;
            ____result.Current = __Current__;
            return ____result;
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name

