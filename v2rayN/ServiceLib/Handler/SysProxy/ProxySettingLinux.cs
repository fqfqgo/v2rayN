namespace ServiceLib.Handler.SysProxy;

public static class ProxySettingLinux
{
    private static readonly string _proxySetFileName = $"{Global.ProxySetLinuxShellFileName.Replace(Global.NamespaceSample, "")}.sh";

    public static async Task SetProxy(string host, int port, string exceptions)
    {
        List<string> args = ["manual", host, port.ToString(), exceptions];
        await ExecCmd(args);
    }

    public static async Task UnsetProxy()
    {
        List<string> args = ["none"];
        await ExecCmd(args);
    }

    public static async Task<bool> IsProxySet(string host, int port)
    {
        if (IsKde())
        {
            return await IsGnomeProxySet(host, port) && await IsKdeProxySet(host, port);
        }
        return await IsGnomeProxySet(host, port);
    }

    private static async Task<bool> IsGnomeProxySet(string host, int port)
    {
        var mode = await Utils.GetCliWrapOutput("gsettings", ["get", "org.gnome.system.proxy", "mode"]);
        if (!string.Equals(mode?.Trim(), "'manual'", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var protocol in new[] { "http", "https", "ftp", "socks" })
        {
            var proxyHost = await Utils.GetCliWrapOutput("gsettings", ["get", $"org.gnome.system.proxy.{protocol}", "host"]);
            var proxyPort = await Utils.GetCliWrapOutput("gsettings", ["get", $"org.gnome.system.proxy.{protocol}", "port"]);
            if (!string.Equals(proxyHost?.Trim(), $"'{host}'", StringComparison.Ordinal)
                || !string.Equals(proxyPort?.Trim(), port.ToString(), StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private static async Task<bool> IsKdeProxySet(string host, int port)
    {
        var command = Environment.GetEnvironmentVariable("KDE_SESSION_VERSION") == "6" ? "kreadconfig6" : "kreadconfig5";
        var proxyType = await Utils.GetCliWrapOutput(command, ["--file", "kioslaverc", "--group", "Proxy Settings", "--key", "ProxyType"]);
        if (proxyType?.Trim() != "1")
        {
            return false;
        }

        var proxy = $"http://{host}:{port}";
        foreach (var key in new[] { "httpProxy", "httpsProxy", "ftpProxy", "socksProxy" })
        {
            var setting = await Utils.GetCliWrapOutput(command, ["--file", "kioslaverc", "--group", "Proxy Settings", "--key", key]);
            if (!string.Equals(setting?.Trim(), proxy, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsKde()
    {
        var desktop = $"{Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")} {Environment.GetEnvironmentVariable("XDG_SESSION_DESKTOP")}";
        return desktop.Contains("KDE", StringComparison.OrdinalIgnoreCase) || desktop.Contains("plasma", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ExecCmd(List<string> args)
    {
        var customSystemProxyScriptPath = AppManager.Instance.Config.SystemProxyItem?.CustomSystemProxyScriptPath;
        var fileName = (customSystemProxyScriptPath.IsNotEmpty() && File.Exists(customSystemProxyScriptPath))
            ? customSystemProxyScriptPath
            : await FileUtils.CreateLinuxShellFile(_proxySetFileName, EmbedUtils.GetEmbedText(Global.ProxySetLinuxShellFileName), false);

        // TODO: temporarily notify which script is being used
        NoticeManager.Instance.SendMessage(fileName);

        await Utils.GetCliWrapOutput(fileName, args);
    }
}
