using System.Net.Http.Headers;

namespace ServiceLib.Handler;

public static class SubscriptionHandler
{
    public static async Task UpdateProcess(
        Config config,
        string subId,
        bool blProxy,
        Func<bool, string, Task> updateFunc,
        Func<SubItem, Task>? decryptFailedFunc = null)
    {
        await updateFunc?.Invoke(false, ResUI.MsgUpdateSubscriptionStart);
        var subItem = await AppManager.Instance.SubItems();

        if (subItem is not { Count: > 0 })
        {
            await updateFunc?.Invoke(false, ResUI.MsgNoValidSubscription);
            return;
        }

        var successCount = 0;
        foreach (var item in subItem)
        {
            try
            {
                if (!IsValidSubscription(item, subId))
                {
                    continue;
                }

                var hashCode = $"{item.Remarks}->";
                if (item.Enabled == false)
                {
                    await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgSkipSubscriptionUpdate}");
                    continue;
                }

                // Create download handler
                var downloadHandle = CreateDownloadHandler(hashCode, updateFunc);
                await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgStartGettingSubscriptions}");

                // Get all subscription content (main subscription + additional subscriptions)
                var (result, decryptFailed) = await DownloadAllSubscriptions(config, item, blProxy, downloadHandle);
                if (decryptFailed)
                {
                    await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgSubscriptionDecryptFailed}");
                    if (decryptFailedFunc != null)
                    {
                        await decryptFailedFunc(item);
                    }
                    continue;
                }

                // Process download result
                if (await ProcessDownloadResult(config, item.Id, result, hashCode, updateFunc))
                {
                    successCount++;
                }

                await updateFunc?.Invoke(false, "-------------------------------------------------------");
            }
            catch (Exception ex)
            {
                var hashCode = $"{item.Remarks}->";
                Logging.SaveLog("UpdateSubscription", ex);
                await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgFailedImportSubscription}: {ex.Message}");
                await updateFunc?.Invoke(false, "-------------------------------------------------------");
            }
        }

        await updateFunc?.Invoke(successCount > 0, $"{ResUI.MsgUpdateSubscriptionEnd}");
    }

    private static bool IsValidSubscription(SubItem item, string subId)
    {
        var id = item.Id.TrimEx();
        var url = item.Url.TrimEx();

        if (id.IsNullOrEmpty() || url.IsNullOrEmpty())
        {
            return false;
        }

        if (subId.IsNotEmpty() && item.Id != subId)
        {
            return false;
        }

        if (!url.StartsWith(Global.HttpsProtocol) && !url.StartsWith(Global.HttpProtocol))
        {
            return false;
        }

        return true;
    }

    private static DownloadService CreateDownloadHandler(string hashCode, Func<bool, string, Task> updateFunc)
    {
        var downloadHandle = new DownloadService();
        downloadHandle.Error += (sender2, args) =>
        {
            updateFunc?.Invoke(false, $"{hashCode}{args.GetException().Message}");
        };
        return downloadHandle;
    }

    private static bool IsEncrypted(HttpHeaders? headers)
    {
        return headers?.TryGetValues("Subscription-Encryption", out var values) == true
               && values.Any(value => value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(string Content, HttpHeaders? Headers)> DownloadSubscriptionContent(DownloadService downloadHandle, string url, bool blProxy, string userAgent)
    {
        var (result, headers) = await downloadHandle.TryDownloadStringWithHeaders(url, blProxy, userAgent);

        // If download with proxy fails, try direct connection
        if (blProxy && result.IsNullOrEmpty())
        {
            (result, headers) = await downloadHandle.TryDownloadStringWithHeaders(url, false, userAgent);
        }

        return (result ?? string.Empty, headers);
    }

    public static async Task<bool> IsUrlDownloadableAsync(string url, string? userAgent)
    {
        url = Utils.GetPunycode(url.TrimEx());
        if (url.IsNullOrEmpty())
        {
            return false;
        }

        var downloadHandle = new DownloadService();
        var (result, _) = await DownloadSubscriptionContent(downloadHandle, url, true, userAgent ?? string.Empty);
        return result.IsNotEmpty();
    }

    private static async Task<(string Result, bool DecryptFailed)> DownloadAllSubscriptions(Config config, SubItem item, bool blProxy, DownloadService downloadHandle)
    {
        // Download main subscription content
        var (result, headers) = await DownloadMainSubscription(config, item, blProxy, downloadHandle);
        if (IsEncrypted(headers))
        {
            if (!TryDecryptSubscription(item.LoginPassword, result, out result))
            {
                return (string.Empty, true);
            }
        }
        else if (result.IsNotEmpty() && Utils.IsBase64String(result))
        {
            result = Utils.Base64Decode(result);
        }

        // Process additional subscription links (if any)
        if (item.ConvertTarget.IsNullOrEmpty() && item.MoreUrl.TrimEx().IsNotEmpty())
        {
            var additional = await DownloadAdditionalSubscriptions(item, result, blProxy, downloadHandle);
            if (additional.DecryptFailed)
            {
                return (string.Empty, true);
            }
            result = additional.Result;
        }

        return (result, false);
    }

    private static async Task<(string Content, HttpHeaders? Headers)> DownloadMainSubscription(Config config, SubItem item, bool blProxy, DownloadService downloadHandle)
    {
        // Prepare subscription URL and download directly
        var url = Utils.GetPunycode(item.Url.TrimEx());

        // If conversion is needed
        if (item.ConvertTarget.IsNotEmpty())
        {
            var subConvertUrl = config.ConstItem.SubConvertUrl.IsNullOrEmpty()
                ? Global.SubConvertUrls.FirstOrDefault()
                : config.ConstItem.SubConvertUrl;

            url = string.Format(subConvertUrl!, Utils.UrlEncode(url));

            if (!url.Contains("target="))
            {
                url += $"&target={item.ConvertTarget}";
            }

            if (!url.Contains("config="))
            {
                url += $"&config={Global.SubConvertConfig.FirstOrDefault()}";
            }
        }

        // Download and return result directly
        return await DownloadSubscriptionContent(downloadHandle, url, blProxy, item.UserAgent);
    }

    private static async Task<(string Result, bool DecryptFailed)> DownloadAdditionalSubscriptions(SubItem item, string mainResult, bool blProxy, DownloadService downloadHandle)
    {
        var result = mainResult;

        // Process additional URL list
        var lstUrl = item.MoreUrl.TrimEx().Split(",") ?? [];
        foreach (var it in lstUrl)
        {
            var url2 = Utils.GetPunycode(it);
            if (url2.IsNullOrEmpty())
            {
                continue;
            }

            var (additionalResult, headers) = await DownloadSubscriptionContent(downloadHandle, url2, blProxy, item.UserAgent);

            if (additionalResult.IsNotEmpty())
            {
                // Process additional subscription results, add to main result
                if (IsEncrypted(headers))
                {
                    if (!TryDecryptSubscription(item.LoginPassword, additionalResult, out additionalResult))
                    {
                        return (string.Empty, true);
                    }
                    result += Environment.NewLine + additionalResult;
                }
                else if (Utils.IsBase64String(additionalResult))
                {
                    result += Environment.NewLine + Utils.Base64Decode(additionalResult);
                }
                else
                {
                    result += Environment.NewLine + additionalResult;
                }
            }
        }

        return (result, false);
    }

    private static bool TryDecryptSubscription(string? loginPassword, string base64Data, out string decrypted)
    {
        decrypted = string.Empty;
        var password = loginPassword?.Trim();
        if (password.IsNullOrEmpty() || base64Data.IsNullOrEmpty())
        {
            return false;
        }

        try
        {
            var encoded = base64Data.Trim()
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "")
                .Replace('_', '/')
                .Replace('-', '+');
            var raw = Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='));
            if (raw.Length <= 16)
            {
                return false;
            }

            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(Utils.GetMd5(password));
            aes.IV = raw[..16];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            decrypted = Encoding.UTF8.GetString(aes.CreateDecryptor().TransformFinalBlock(raw, 16, raw.Length - 16));
            return decrypted.IsNotEmpty();
        }
        catch (Exception ex)
        {
            Logging.SaveLog("SubscriptionDecryptFailed", ex);
            return false;
        }
    }

    private static async Task<bool> ProcessDownloadResult(Config config, string id, string result, string hashCode, Func<bool, string, Task> updateFunc)
    {
        if (result.IsNullOrEmpty())
        {
            await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgSubscriptionDecodingFailed}");
            return false;
        }

        await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgGetSubscriptionSuccessfully}");

        // If result is too short, display content directly
        if (result.Length < 99)
        {
            await updateFunc?.Invoke(false, $"{hashCode}{result}");
        }

        await updateFunc?.Invoke(false, $"{hashCode}{ResUI.MsgStartParsingSubscription}");

        // Add servers to configuration
        var ret = await ConfigHandler.AddBatchServers(config, result, id, true);
        if (ret <= 0)
        {
            Logging.SaveLog("FailedImportSubscription");
            Logging.SaveLog(result);
        }

        // Update completion message
        await updateFunc?.Invoke(false, ret > 0
                ? $"{hashCode}{ResUI.MsgUpdateSubscriptionEnd}"
                : $"{hashCode}{ResUI.MsgFailedImportSubscription}");

        return ret > 0;
    }
}
