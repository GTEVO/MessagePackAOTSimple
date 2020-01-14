using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CommonLib.Network
{
    public enum NetworkCmd
    {
        ConnectTo = 1 << 2,
        DependableTransform = 1 << 3,
        KeepAlive = 1 << 4,
        DisConnect = 1 << 5,
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
