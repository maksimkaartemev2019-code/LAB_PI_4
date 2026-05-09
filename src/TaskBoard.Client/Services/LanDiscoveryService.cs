using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using TaskBoard.Client.Models;

namespace TaskBoard.Client.Services;

public sealed class LanDiscoveryService : IAsyncDisposable
{
    private const int DiscoveryPort = 5056;
    private const string Prefix = "TEAM_TASK_BOARD";
    private readonly ConcurrentDictionary<string, DiscoveredBoardHost> hosts = [];
    private CancellationTokenSource? listenCts;
    private CancellationTokenSource? announceCts;
    private Task? listenTask;
    private Task? announceTask;

    public event Action<DiscoveredBoardHost>? HostDiscovered;

    public IReadOnlyList<DiscoveredBoardHost> Hosts => hosts.Values
        .OrderByDescending(host => host.LastSeenAt)
        .ToList();

    public void StartListening()
    {
        if (listenTask is not null)
        {
            return;
        }

        listenCts = new CancellationTokenSource();
        listenTask = Task.Run(() => ListenAsync(listenCts.Token));
    }

    public void StartAnnouncing(string serverUrl, string serverName)
    {
        StopAnnouncing();
        announceCts = new CancellationTokenSource();
        announceTask = Task.Run(() => AnnounceAsync(serverUrl, serverName, announceCts.Token));
    }

    public void StopAnnouncing()
    {
        announceCts?.Cancel();
        announceCts = null;
        announceTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        listenCts?.Cancel();
        announceCts?.Cancel();
        if (listenTask is not null)
        {
            await SafeWaitAsync(listenTask);
        }

        if (announceTask is not null)
        {
            await SafeWaitAsync(announceTask);
        }
    }

    public static string GetBestLocalServerUrl(int port)
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up)
            .Where(network => network.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
            .SelectMany(network => network.GetIPProperties().UnicastAddresses)
            .Select(address => address.Address)
            .FirstOrDefault(IsPrivateIPv4);

        return $"http://{address ?? IPAddress.Loopback}:{port}";
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await udp.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var message = Encoding.UTF8.GetString(result.Buffer);
            var parts = message.Split('|', 3);
            if (parts.Length != 3 || parts[0] != Prefix || !Uri.TryCreate(parts[2], UriKind.Absolute, out _))
            {
                continue;
            }

            var host = new DiscoveredBoardHost
            {
                Name = parts[1],
                Url = parts[2],
                LastSeenAt = DateTimeOffset.UtcNow
            };
            hosts.AddOrUpdate(host.Url, host, (_, existing) =>
            {
                existing.Name = host.Name;
                existing.LastSeenAt = host.LastSeenAt;
                return existing;
            });
            HostDiscovered?.Invoke(host);
        }
    }

    private static async Task AnnounceAsync(string serverUrl, string serverName, CancellationToken cancellationToken)
    {
        using var udp = new UdpClient { EnableBroadcast = true };
        var payload = Encoding.UTF8.GetBytes($"{Prefix}|{serverName}|{serverUrl}");
        var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await udp.SendAsync(payload, endpoint, cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static bool IsPrivateIPv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31
            || bytes[0] == 192 && bytes[1] == 168;
    }

    private static async Task SafeWaitAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
