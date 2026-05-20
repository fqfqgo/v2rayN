using Avalonia.Input.Platform;

namespace v2rayN.Desktop.Common;

internal class AvaUtils
{
    public static async Task<string?> GetClipboardData(Window owner)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(owner)?.Clipboard;
            if (clipboard == null)
            {
                return null;
            }

            return await clipboard.TryGetTextAsync();
        }
        catch
        {
            return null;
        }
    }

    public static async Task SetClipboardData(Visual? visual, string strData)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(visual)?.Clipboard;
            if (clipboard == null)
            {
                return;
            }

            await clipboard.SetTextAsync(strData);
        }
        catch
        {
        }
    }

    public static WindowIcon GetAppIcon(Config config)
    {
        var proxyOn = config.SystemProxyItem.SysProxyType is ESysProxyType.ForcedChange or ESysProxyType.Pac;
        var tunOn = config.TunModeItem.EnableTun;
        var index = (proxyOn || tunOn ? 1 : 0) + 1;
        var fileName = Utils.GetPath($"NotifyIcon{index}.ico");
        if (File.Exists(fileName))
        {
            return new(fileName);
        }

        var uri = new Uri(Path.Combine(Global.AvaAssets, $"NotifyIcon{index}.ico"));
        using var bitmap = new Bitmap(AssetLoader.Open(uri));
        return new(bitmap);
    }
}
