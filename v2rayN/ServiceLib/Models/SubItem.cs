namespace ServiceLib.Models;

[Serializable]
public class SubItem
{
    [PrimaryKey]
    public string Id { get; set; }

    public string Remarks { get; set; }

    public string Url { get; set; }

    public string MoreUrl { get; set; }

    private string? _loginPassword;
    /// <summary>网站登录/订阅解密密码，读写时自动去除首尾空格</summary>
    public string? LoginPassword { get => _loginPassword; set => _loginPassword = value?.Trim(); }

    public bool Enabled { get; set; } = true;

    public string UserAgent { get; set; } = string.Empty;

    public int Sort { get; set; }

    public string? Filter { get; set; }

    public int AutoUpdateInterval { get; set; }

    public long UpdateTime { get; set; }

    public string? ConvertTarget { get; set; }

    public string? PrevProfile { get; set; }

    public string? NextProfile { get; set; }

    public int? PreSocksPort { get; set; }

    public string? Memo { get; set; }
}
