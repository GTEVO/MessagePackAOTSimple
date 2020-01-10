using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using UnityEngine;
using System.Threading.Tasks.Dataflow;


namespace Server
{
    [MessagePackObject]
    public class MessagePackage
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }

    [MessagePackObject]
    public class Position
    {
        [Key(0)]
        public Vector3 Last { get; set; }

        [Key(1)]
        public Vector3 Current { get; set; }
    }


    public class UdpServer
    {

        public struct RecvResult
        {
            public int len;
            public EndPoint remote;
        }

        byte[] _buffer;
        EndPoint _remoteEP;
        CancellationTokenSource _cancellationTokenSource;

        Socket _socket;

        //  BufferBlock<>

        public void Start()
        {
            _buffer = new byte[ushort.MaxValue];
            _remoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            _cancellationTokenSource = new CancellationTokenSource();

            var ip = new IPEndPoint(IPAddress.Any, 8000);
            _socket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(ip);
            var recvTask = new Task(async () => {
                while (!_cancellationTokenSource.IsCancellationRequested) {
                    var result = await Task.Factory.FromAsync(BeginRecvFrom, EndRecvFrom, _socket, TaskCreationOptions.AttachedToParent);
                    var m = new ReadOnlyMemory<byte>(_buffer, 0, result.len);
                    var pos = MessagePackSerializer.Deserialize<MessagePackage>(m);
                    var p = MessagePackSerializer.Deserialize<Position>(pos.Data);
                    Console.WriteLine(result);
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            recvTask.Start();
        }

        private IAsyncResult BeginRecvFrom(AsyncCallback callback, object state)
        {
            var result = _socket.BeginReceiveFrom(_buffer, 0, ushort.MaxValue, SocketFlags.None, ref _remoteEP, callback, state);
            return result;
        }

        private RecvResult EndRecvFrom(IAsyncResult result)
        {
            var recvBytes = _socket.EndReceiveFrom(result, ref _remoteEP);
            var rr = new RecvResult {
                len = recvBytes,
                remote = _remoteEP,
            };
            return rr;
        }

    }
}
