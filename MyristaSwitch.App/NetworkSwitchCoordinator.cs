using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MyristaSwitch.App;

internal sealed class NetworkSwitchCoordinator : IDisposable
{
    private const int Port = 37842;
    private readonly string _machineId;
    private readonly UdpClient _sender = new();
    private readonly UdpClient? _receiver;
    private readonly CancellationTokenSource _cancellation = new();

    public NetworkSwitchCoordinator(string machineId)
    {
        _machineId = machineId;
        _sender.EnableBroadcast = true;
        try
        {
            _receiver = new UdpClient(AddressFamily.InterNetwork);
            _receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _receiver.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
            _ = ReceiveLoopAsync(_cancellation.Token);
        }
        catch
        {
            _receiver?.Dispose();
            _receiver = null;
        }
    }

    public event EventHandler<RemoteActiveEventArgs>? RemoteActive;

    public async Task AnnounceActiveAsync(CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Serialize(new SwitchMessage(_machineId, Environment.MachineName, DateTimeOffset.UtcNow));
        var bytes = Encoding.UTF8.GetBytes(message);
        var endpoint = new IPEndPoint(IPAddress.Broadcast, Port);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await _sender.SendAsync(bytes, endpoint, cancellationToken);
            await Task.Delay(150, cancellationToken);
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _sender.Dispose();
        _receiver?.Dispose();
        _cancellation.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_receiver is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _receiver.ReceiveAsync(cancellationToken);
                var json = Encoding.UTF8.GetString(result.Buffer);
                var message = JsonSerializer.Deserialize<SwitchMessage>(json);
                if (message is null || string.Equals(message.MachineId, _machineId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                RemoteActive?.Invoke(this, new RemoteActiveEventArgs(message.MachineName));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // UDP coordination is opportunistic; ignore malformed packets and keep listening.
            }
        }
    }

    private sealed record SwitchMessage(string MachineId, string MachineName, DateTimeOffset Timestamp);
}

internal sealed class RemoteActiveEventArgs(string machineName) : EventArgs
{
    public string MachineName { get; } = machineName;
}
