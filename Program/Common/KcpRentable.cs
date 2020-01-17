using System.Buffers;
using System.Net.Sockets.Kcp;

namespace CommonLib
{
    public class KcpRentable : Singleton<KcpRentable>, IRentable
    {
        IMemoryOwner<byte> IRentable.RentBuffer(int length)
        {
            return MemoryPool<byte>.Shared.Rent(length);
        }
    }
}
