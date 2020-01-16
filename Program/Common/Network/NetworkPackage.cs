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
        public const byte ConnectTo = 1 << 2;
        public const byte DependableTransform = 1 << 3;
        public const byte KeepAlive = 1 << 4;
        public const byte DisConnect = 1 << 5;
    }

    public struct RecvResult
    {
        public int len;
        public EndPoint remote;
    }

    public interface INetworkPackage
    {
        IMemoryOwner<byte> MemoryOwner { get; set; }
    }

    public class NetworkPackPolicy<T> : IPooledObjectPolicy<T> where T : INetworkPackage, new()
    {
        T IPooledObjectPolicy<T>.Create()
        {
            return new T();
        }

        bool IPooledObjectPolicy<T>.Return(T obj)
        {
            obj.MemoryOwner.Dispose();
            return true;
        }
    }


    public class NetworkPackage : NetworkBasePackage
    {
        public static new ObjectPool<NetworkPackage> Pool = ObjectPool.Create(new NetworkPackPolicy<NetworkPackage>());
        public EndPoint Remote { get; set; }
    }

    public class NetworkBasePackage : INetworkPackage
    {
        public static ObjectPool<NetworkBasePackage> Pool = ObjectPool.Create(new NetworkPackPolicy<NetworkBasePackage>());

        public int Size { get; set; }
        public IMemoryOwner<byte> MemoryOwner { get; set; }
    }
}
