using System.Net;
using System.Net.Sockets;
using ServiceLib.Enums;
using ServiceLib.Events;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Resx;

namespace ServiceLib.Handler;

/// <summary>
/// 当 mixed 主监听端口（及关联的 socks2/socks3）被占用时，自动递增端口、保存配置并触发重载；系统代理通过 AppManager.GetLocalPort 读取新端口。
/// </summary>
public static class MixedListenPortRecoveryHandler
{
    private static readonly SemaphoreSlim _recoverGate = new(1, 1);
    private static int _sessionBumpCount;
    private const int MaxSessionBumps = 64;
    private const string _tag = "MixedListenPortRecovery";
    private static readonly int _maxBasePort = 65535 - (int)EInboundProtocol.socks3;

    /// <summary>与核心常见报错匹配（不区分大小写）。</summary>
    public static bool LooksLikeInboundBindFailure(string? text)
    {
        if (text.IsNullOrEmpty())
        {
            return false;
        }

        var t = text.ToLowerInvariant();
        if (t.Contains("failed to listen"))
        {
            return true;
        }

        if (t.Contains("bind: address already in use"))
        {
            return true;
        }

        if (t.Contains("address already in use"))
        {
            return true;
        }

        // Windows
        if (t.Contains("only one usage of each socket address"))
        {
            return true;
        }

        // sing-box / 部分运行时
        if (t.Contains("listen tcp") && t.Contains("bind"))
        {
            return true;
        }

        return false;
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

    /// <summary>与当前 InItem 下实际生成的 mixed 监听地址一致性检测。</summary>
    private static bool IsPrimaryMixedPortStackAvailable(InItem item, int basePort)
    {
        if (basePort <= 0 || basePort > _maxBasePort)
        {
            return false;
        }

        // 主 mixed：与 SingboxInboundService / V2rayInboundService 一致
        if (item.AllowLANConn && !item.NewPort4LAN)
        {
            if (!TryBindTcp(IPAddress.Any, basePort))
            {
                return false;
            }
        }
        else
        {
            if (!TryBindTcp(IPAddress.Parse(Global.Loopback), basePort))
            {
                return false;
            }
        }

        if (item.SecondLocalPortEnabled)
        {
            var p2 = basePort + (int)EInboundProtocol.socks2;
            if (p2 > 65535 || !TryBindTcp(IPAddress.Parse(Global.Loopback), p2))
            {
                return false;
            }
        }

        if (item.AllowLANConn && item.NewPort4LAN)
        {
            var p3 = basePort + (int)EInboundProtocol.socks3;
            if (p3 > 65535 || !TryBindTcp(IPAddress.Any, p3))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>若当前 LocalPort 不可用，则递增直到可用；修改 config。返回是否变更。</summary>
    public static bool TryBumpPrimaryMixedPortIfCurrentBusy(Config config, out int oldPort, out int newPort)
    {
        oldPort = 0;
        newPort = 0;
        var inbound = config.Inbound?.FirstOrDefault(t => t.Protocol == nameof(EInboundProtocol.socks));
        if (inbound is null)
        {
            return false;
        }

        oldPort = inbound.LocalPort;
        if (IsPrimaryMixedPortStackAvailable(inbound, inbound.LocalPort))
        {
            return false;
        }

        for (var p = inbound.LocalPort + 1; p <= _maxBasePort; p++)
        {
            if (!IsPrimaryMixedPortStackAvailable(inbound, p))
            {
                continue;
            }

            inbound.LocalPort = p;
            newPort = p;
            return true;
        }

        return false;
    }

    /// <summary>在已出现监听失败日志后，从 LocalPort+1 起寻找可用端口。</summary>
    public static bool TryReassignPrimaryMixedPortAfterBindError(Config config, out int oldPort, out int newPort)
    {
        oldPort = 0;
        newPort = 0;
        var inbound = config.Inbound?.FirstOrDefault(t => t.Protocol == nameof(EInboundProtocol.socks));
        if (inbound is null)
        {
            return false;
        }

        oldPort = inbound.LocalPort;
        for (var p = oldPort + 1; p <= _maxBasePort; p++)
        {
            if (!IsPrimaryMixedPortStackAvailable(inbound, p))
            {
                continue;
            }

            inbound.LocalPort = p;
            newPort = p;
            return true;
        }

        return false;
    }

    /// <summary>异步处理核心日志中的端口占用提示，保存配置并请求 Reload（Reload 内会更新系统代理）。</summary>
    public static void ScheduleRecoverFromCoreLog(Config config, string? logLine)
    {
        if (!LooksLikeInboundBindFailure(logLine))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await _recoverGate.WaitAsync();
            try
            {
                if (_sessionBumpCount >= MaxSessionBumps)
                {
                    return;
                }

                if (!TryReassignPrimaryMixedPortAfterBindError(config, out var oldPort, out var newPort))
                {
                    return;
                }

                if (newPort == oldPort)
                {
                    return;
                }

                _sessionBumpCount++;
                if (await ConfigHandler.SaveConfig(config) != 0)
                {
                    var inbound = config.Inbound?.FirstOrDefault(t => t.Protocol == nameof(EInboundProtocol.socks));
                    if (inbound is not null)
                    {
                        inbound.LocalPort = oldPort;
                    }

                    return;
                }

                Logging.SaveLog($"{_tag}: primary mixed listen {oldPort} -> {newPort} (bind conflict from log)");
                NoticeManager.Instance.SendMessageEx(string.Format(ResUI.TipMixedListenPortAutoAdjusted, oldPort, newPort));
                AppEvents.ReloadRequested.Publish();
            }
            finally
            {
                _recoverGate.Release();
            }
        });
    }
}
