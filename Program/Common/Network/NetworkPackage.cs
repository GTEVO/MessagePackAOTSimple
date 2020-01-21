using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Net;
using System.Text;

namespace CommonLib.Network
{
    public static class NetworkCmd
    {
        public const byte FIN = 1;
        public const byte SYN = 1 << 1;
        public const byte RESET = 1 << 2;
        public const byte PUSH = 1 << 3;
        public const byte ACK = 1 << 4;
    }

    public struct RecvResult
    {
        public int len;
        public EndPoint remote;
    }

    public interface IDataPackage
    {
        int Offset { get; set; }
        int Lenght { get; set; }
        IMemoryOwner<byte> MemoryOwner { get; set; }
    }

    public class DataPackagePolicy<T> : IPooledObjectPolicy<T> where T : IDataPackage, new()
    {
        T IPooledObjectPolicy<T>.Create()
        {
            return new T();
        }

        bool IPooledObjectPolicy<T>.Return(T obj)
        {
            obj.Offset = 0;
            obj.Lenght = 0;
            obj.MemoryOwner = null;

            return true;
        }
    }

    public class LinkPackage : DataPackage
    {
        public static new ObjectPool<LinkPackage> Pool = ObjectPool.Create(new DataPackagePolicy<LinkPackage>());

        public IReliableDataLink Link { get; set; }
    }

    public class NetworkPackage : DataPackage
    {
        public static new ObjectPool<NetworkPackage> Pool = ObjectPool.Create(new DataPackagePolicy<NetworkPackage>());

        public EndPoint Remote { get; set; }
    }

    public class DataPackage : IDataPackage
    {
        public static ObjectPool<DataPackage> Pool = ObjectPool.Create(new DataPackagePolicy<DataPackage>());

        public int Offset { get; set; }
        public int Lenght { get; set; }
        public IMemoryOwner<byte> MemoryOwner { get; set; }
    }
}
