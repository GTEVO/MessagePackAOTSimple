using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets.Kcp;
using System.Text;
using System.Threading.Tasks;

namespace CommonLib.Network
{
    public interface IReliableDataLink
    {
        Task SendToRemoteAsync(ReadOnlyMemory<byte> buffer);
    }

    public interface IKcpLink : IReliableDataLink
    {
        event Action<IMemoryOwner<byte>, int, IReliableDataLink> OnRecvKcpPackage;

        uint Conv { get; }
        int Rtt { get; }

        void Run(uint conv, IKcpCallback kcpCallback);
        void Stop(Action onCancel = null);
        Task RecvFromRemoteAsync(ReadOnlyMemory<byte> buffer);
    }

}
