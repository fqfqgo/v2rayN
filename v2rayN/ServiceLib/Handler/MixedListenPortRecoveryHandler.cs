using System.Net;
using System.Net.Sockets;

namespace ServiceLib.Handler;

public static class MixedListenPortRecoveryHandler
{
    private static readonly SemaphoreSlim _recoverGate = new(1, 1);
    private static readonly int _maxBasePort = 65535 - (int)EInboundProtocol.socks3;
    private const int MaxSessionBumps = 64;
    private static int _sessionBumpCount;

    public static bool TryBumpPrimaryMixedPortIfCurrentBusy(Config config, out int oldPort, out int newPort)
    {
        oldPort = newPort = 0;
        var inbound = config.Inbound?.FirstOrDefault(t => t.Protocol == nameof(EInboundProtocol.socks));
        if (inbound is null)
        {
            return false;
        }

        oldPort = inbound.LocalPort;
        if (IsPrimaryMixedPortStackAvailable(inbound, oldPort))
        {
            return false;
        }

        return TryAssignNextAvailablePort(inbound, oldPort, out newPort);
    }

    public static async Task<bool> RecoverFromCoreLog(Config config, string? logLine)
    {
        if (!LooksLikeInboundBindFailure(logLine))
        {
            return false;
        }

        await _recoverGate.WaitAsync();
        try
        {
            if (_sessionBumpCount >= MaxSessionBumps)
            {
                return false;
            }

            var inbound = config.Inbound?.FirstOrDefault(t => t.Protocol == nameof(EInboundProtocol.socks));
            if (inbound is null)
            {
                return false;
            }

            var oldPort = inbound.LocalPort;
            if (!TryAssignNextAvailablePort(inbound, oldPort, out var newPort))
            {
                return false;
            }

            if (await ConfigHandler.SaveConfig(config) != 0)
            {
                inbound.LocalPort = oldPort;
                return false;
            }

            _sessionBumpCount++;
            Logging.SaveLog($"MixedListenPortRecovery: primary mixed listen {oldPort} -> {newPort} (bind conflict)");
            NoticeManager.Instance.SendMessageEx(string.Format(ResUI.TipMixedListenPortAutoAdjusted, oldPort, newPort));
            return true;
        }
        finally
        {
            _recoverGate.Release();
        }
    }

    private static bool TryAssignNextAvailablePort(InItem inbound, int oldPort, out int newPort)
    {
        for (var port = oldPort + 1; port <= _maxBasePort; port++)
        {
            if (!IsPrimaryMixedPortStackAvailable(inbound, port))
            {
                continue;
            }

            inbound.LocalPort = newPort = port;
            return true;
        }

        newPort = 0;
        return false;
    }

    private static bool IsPrimaryMixedPortStackAvailable(InItem inbound, int basePort)
    {
        if (basePort <= 0 || basePort > _maxBasePort)
        {
            return false;
        }

        if (!TryBindTcp(inbound.AllowLANConn && !inbound.NewPort4LAN ? IPAddress.Any : IPAddress.Loopback, basePort))
        {
            return false;
        }
        if (inbound.SecondLocalPortEnabled && !TryBindTcp(IPAddress.Loopback, basePort + (int)EInboundProtocol.socks2))
        {
            return false;
        }
        return !inbound.AllowLANConn
            || !inbound.NewPort4LAN
            || TryBindTcp(IPAddress.Any, basePort + (int)EInboundProtocol.socks3);
    }

    private static bool TryBindTcp(IPAddress address, int port)
    {
        try
        {
            var listener = new TcpListener(address, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool LooksLikeInboundBindFailure(string? text)
    {
        if (text.IsNullOrEmpty())
        {
            return false;
        }

        var value = text.ToLowerInvariant();
        return value.Contains("failed to listen")
            || value.Contains("address already in use")
            || value.Contains("only one usage of each socket address")
            || value.Contains("listen tcp") && value.Contains("bind");
    }
}
