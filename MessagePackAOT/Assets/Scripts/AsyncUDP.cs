using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System;

public class AsyncUDP : MonoBehaviour
{
    byte[] _buffer;
    EndPoint _remoteEP;
    CancellationTokenSource _cancellationTokenSource;

    Socket _socket;

    public void OnClickStart()
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
                Debug.Log(result);
            }
        }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
        recvTask.ContinueWith(task => {
            Debug.LogError(task.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted).Start();
    }

    private IAsyncResult BeginRecvFrom(AsyncCallback callback, object state)
    {
        var result = _socket.BeginReceiveFrom(_buffer, 0, ushort.MaxValue, SocketFlags.None, ref _remoteEP, callback, state);
        return result;
    }

    private IAsyncResult EndRecvFrom(IAsyncResult result)
    {
        //  var recvBytes = _socket.EndReceiveFrom(result, ref _remoteEP);
        return result;
    }

    public void OnClickStop()
    {
        _cancellationTokenSource.Cancel();
        OnDestroy();
    }

    private void OnDestroy()
    {
        if (_socket != null) {
            _socket.Close();
            _socket.Dispose();
        }
    }
}
