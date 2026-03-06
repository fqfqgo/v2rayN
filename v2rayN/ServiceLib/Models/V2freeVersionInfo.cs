namespace ServiceLib.Models;

/// <summary>
/// 本站 version.json 格式，用于 v2rayN 检查更新
/// 示例: { "version": "7.19.1" }
/// </summary>
public class V2freeVersionInfo
{
    public string? Version { get; set; }
}
