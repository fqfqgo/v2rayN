namespace ServiceLib.Handler.SysProxy;

public static class ProxySettingOSX
{
    private static readonly string _proxySetFileName = $"{Global.ProxySetOSXShellFileName.Replace(Global.NamespaceSample, "")}.sh";

    public static async Task SetProxy(string host, int port, string exceptions)
    {
        List<string> args = ["set", host, port.ToString()];
        if (exceptions.IsNotEmpty())
        {
            args.AddRange(exceptions.Split(','));
        }

        await ExecCmd(args);
    }

    public static async Task UnsetProxy()
    {
        List<string> args = ["clear"];
        await ExecCmd(args);
    }

    public static async Task<bool> IsProxySet(string host, int port)
    {
        var services = await Utils.GetCliWrapOutput("networksetup", "-listallnetworkservices");
        if (services.IsNullOrEmpty())
        {
            return false;
        }

        foreach (var service in services.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(t => !t.Contains('*')))
        {
            foreach (var type in new[] { "-getwebproxy", "-getsecurewebproxy", "-getsocksfirewallproxy" })
            {
                var setting = await Utils.GetCliWrapOutput("networksetup", [type, service]);
                if (setting.IsNullOrEmpty()
                    || !setting.Contains("Enabled: Yes", StringComparison.OrdinalIgnoreCase)
                    || !setting.Contains($"Server: {host}", StringComparison.OrdinalIgnoreCase)
                    || !setting.Contains($"Port: {port}", StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static async Task ExecCmd(List<string> args)
    {
        var customSystemProxyScriptPath = AppManager.Instance.Config.SystemProxyItem?.CustomSystemProxyScriptPath;
        var fileName = (customSystemProxyScriptPath.IsNotEmpty() && File.Exists(customSystemProxyScriptPath))
            ? customSystemProxyScriptPath
            : await FileUtils.CreateLinuxShellFile(_proxySetFileName, EmbedUtils.GetEmbedText(Global.ProxySetOSXShellFileName), false);

        // TODO: temporarily notify which script is being used
        NoticeManager.Instance.SendMessage(fileName);

        await Utils.GetCliWrapOutput(fileName, args);
    }
}
