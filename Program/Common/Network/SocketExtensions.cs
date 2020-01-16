using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommonLib.Network
{
    public static class SocketExtensions
    {
        public static void IgnoreRemoteHostClosedException(this Socket socket)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }
        }

    }
}
