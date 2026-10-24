using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Transports.Enums;
using TouchSocket.Core;
using TouchSocket.SerialPorts;

namespace DevKit.Transports;

public class SerialPortTransport : IClientTransport
{
    private string _portName;
    private SerialPortOption _option;
    private SerialPortClient _client;
    private TaskCompletionSource<bool> _closedTcs;
    private bool _activeClose;
    private bool _disposed;
    
    public event Action<byte[]> DataReceived;
    public event Action<TransportState> StateChanged;
    
    public void SetRemoteEndPoint(string address, int port, string path = null)
    {
        
    }

    public void SetPortName(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new ArgumentException(@"串口名不能为空", nameof(portName));
        }
        if (_client != null)
        {
            throw new InvalidOperationException("请先关闭串口，再修改端口");
        }
        _portName = portName.Trim();
    }

    public void SetSerialPortOption(SerialPortOption option)
    {
        if (_client != null)
        {
            throw new InvalidOperationException("请先关闭串口，再修改参数");
        }
        _option = option;
    }
    
    public async Task ConnectAsync()
    {
        ThrowIfDisposed();
        if (_client != null)
        {
            throw new InvalidOperationException("串口已经打开");
        }

        var option = _option ?? throw new InvalidOperationException("请先设置串口参数");
        option.PortName = _portName;

        StateChanged?.Invoke(TransportState.Connecting);

        var client = new SerialPortClient();
        var closedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _client = client;
        _closedTcs = closedTcs;
        _activeClose = false;

        try
        {
            await client.SetupAsync(new TouchSocketConfig().SetSerialPortOption(o =>
            {
                o.PortName = option.PortName;
                o.BaudRate = option.BaudRate;
                o.DataBits = option.DataBits;
                o.Parity = option.Parity;
                o.StopBits = option.StopBits;
                o.Handshake = option.Handshake;
            })).ConfigureAwait(false);

            BindEvents(client, closedTcs);

            await client.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
            StateChanged?.Invoke(TransportState.Connected);
        }
        catch
        {
            client.Dispose();
            closedTcs.TrySetResult(true);

            if (ReferenceEquals(_client, client))
            {
                _client = null;
                _closedTcs = null;
            }

            StateChanged?.Invoke(TransportState.Error);
            throw;
        }
    }
    
    public async Task SendAsync(byte[] payload)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(payload);

        var client = _client;
        if (client == null)
        {
            throw new InvalidOperationException("串口尚未打开");
        }

        await client.SendAsync(payload).ConfigureAwait(false);
    }
    
    public async Task DisconnectAsync()
    {
        var client = _client;
        var closedTcs = _closedTcs;
        if (client == null)
        {
            return;
        }

        _activeClose = true;
        try
        {
            await client.CloseAsync("主动关闭串口").ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine(@"串口主动关闭异常：{0}", e);
        }

        if (closedTcs != null)
        {
            await closedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _activeClose = true;

        var client = _client;
        _client = null;
        _closedTcs?.TrySetResult(true);
        client?.Dispose();
    }
    
    private void BindEvents(SerialPortClient client, TaskCompletionSource<bool> closedTcs)
    {
        client.Received = (_, e) =>
        {
            DataReceived?.Invoke(e.Memory.ToArray());
            return EasyTask.CompletedTask;
        };

        client.Closing = (_, _) =>
        {
            _activeClose = true;
            return EasyTask.CompletedTask;
        };

        client.Closed = (c, _) =>
        {
            var isCurrent = ReferenceEquals(_client, c);
            if (isCurrent)
            {
                _client = null;
                _closedTcs = null;
                StateChanged?.Invoke(_activeClose ? TransportState.Disconnected : TransportState.Error);
            }

            closedTcs.TrySetResult(true);
            c.Dispose();
            return EasyTask.CompletedTask;
        };
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(nameof(SerialPortTransport));
    }
}