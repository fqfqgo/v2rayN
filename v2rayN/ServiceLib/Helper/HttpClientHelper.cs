using System.Net.Http.Headers;
using System.Net.Mime;

namespace ServiceLib.Helper;

/// <summary>
/// </summary>
public class HttpClientHelper
{
    private static readonly Lazy<HttpClientHelper> _instance = new(() =>
    {
        SocketsHttpHandler handler = new() { UseCookies = false };
        HttpClientHelper helper = new(new HttpClient(handler));
        return helper;
    });

    public static HttpClientHelper Instance => _instance.Value;
    private readonly HttpClient httpClient;

    private HttpClientHelper(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<string?> TryGetAsync(string url)
    {
        if (url.IsNullOrEmpty())
        {
            return null;
        }

        try
        {
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 校验链接是否可达：拿到任意 HTTP 响应（含 403/503）即视为可达；
    /// 仅域名无法解析、连接超时、拒绝连接等才返回 false
    /// </summary>
    public async Task<bool> CheckReachableAsync(string url, int timeoutSeconds = 15)
    {
        if (url.IsNullOrEmpty())
        {
            return false;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetAsync(string url)
    {
        if (url.IsNullOrEmpty())
        {
            return null;
        }
        return await httpClient.GetStringAsync(url);
    }

    public async Task PutAsync(string url, Dictionary<string, string> headers)
    {
        var jsonContent = JsonUtils.Serialize(headers);
        var content = new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json);

        await httpClient.PutAsync(url, content);
    }

    public async Task PatchAsync(string url, Dictionary<string, string> headers)
    {
        var myContent = JsonUtils.Serialize(headers);
        var buffer = Encoding.UTF8.GetBytes(myContent);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        await httpClient.PatchAsync(url, byteContent);
    }

    public async Task DeleteAsync(string url)
    {
        await httpClient.DeleteAsync(url);
    }
}
