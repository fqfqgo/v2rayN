namespace ServiceLib.Handler;

public static class ConnectionHandler
{
    private static readonly string _tag = "ConnectionHandler";

    public static async Task<string> RunAvailabilityCheck()
    {
        var time = await GetRealPingTimeInfo();
        var ip = time > 0 ? await GetIPInfo() ?? Global.None : Global.None;

        return string.Format(ResUI.TestMeOutput, time, ip);
    }

    private static async Task<string?> GetIPInfo()
    {
        var url = AppManager.Instance.Config.SpeedTestItem.IPAPIUrl;
        if (url.IsNullOrEmpty())
        {
            return null;
        }

        var downloadHandle = new DownloadService();
        var result = await downloadHandle.TryDownloadString(url, true, "");
        if (result == null)
        {
            return null;
        }

        var ipInfo = JsonUtils.Deserialize<IPAPIInfo>(result);
        if (ipInfo == null)
        {
            return null;
        }

        var ip = ipInfo.ip ?? ipInfo.clientIp ?? ipInfo.ip_addr ?? ipInfo.query;
        var country = ipInfo.country_code ?? ipInfo.country ?? ipInfo.countryCode ?? ipInfo.location?.country_code;

        return $"({country ?? "unknown"}) {ip}";
    }

    private static async Task<int> GetRealPingTimeInfo()
    {
        var responseTime = -1;
        try
        {
            var port = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
            var webProxy = new WebProxy($"socks5://{Global.Loopback}:{port}");
            var url = AppManager.Instance.Config.SpeedTestItem.SpeedPingTestUrl;

            for (var i = 0; i < 2; i++)
            {
                responseTime = await GetRealPingTime(url, webProxy, 10);
                if (responseTime > 0)
                {
                    break;
                }
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            return -1;
        }
        return responseTime;
    }

    public static async Task<int> GetRealPingTime(string url, IWebProxy? webProxy, int downloadTimeout)
    {
        var responseTime = -1;
        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(downloadTimeout));
            using var client = new HttpClient(new SocketsHttpHandler()
            {
                Proxy = webProxy,
                UseProxy = webProxy != null
            });

            List<int> oneTime = new();
            for (var i = 0; i < 2; i++)
            {
                try
                {
                    var timer = Stopwatch.StartNew();
                    var response = await client.GetAsync(url, cts.Token).ConfigureAwait(false);
                    timer.Stop();
                    
                    // 只有成功响应才记录时间
                    if (response.IsSuccessStatusCode)
                    {
                        oneTime.Add((int)timer.Elapsed.TotalMilliseconds);
                    }
                }
                catch
                {
                    // 忽略单次请求失败，继续下一次
                }
                await Task.Delay(100);
            }
            
            // 修复：如果没有成功的结果，返回 -1
            responseTime = oneTime.Count > 0 
                ? oneTime.OrderBy(x => x).First() 
                : -1;
        }
        catch
        {
            // 异常时返回 -1
        }
        return responseTime;
    }
}
