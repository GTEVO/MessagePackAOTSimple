using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CommonLib.Network
{
    public struct RecvResult
    {
        public int len;
        public EndPoint remote;
    }

    public class NetworkPackPolicy : IPooledObjectPolicy<NetworkPack>
    {
        NetworkPack IPooledObjectPolicy<NetworkPack>.Create()
        {
            return new NetworkPack();
        }

        bool IPooledObjectPolicy<NetworkPack>.Return(NetworkPack obj)
        {
            obj.bytes.Dispose();
            return true;
        }
    }

    public class NetworkPack
    {
        public static ObjectPool<NetworkPack> NetworkPackPool = ObjectPool.Create(new NetworkPackPolicy());

        public EndPoint remote;
        public int size;
        public IMemoryOwner<byte> bytes;
    }
}
