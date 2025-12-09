namespace Sinol.DicomViewer.Models;

/// <summary>
/// 系统配置模型
/// </summary>
public class SystemConfig
{
    /// <summary>
    /// API 设置
    /// </summary>
    public ApiSettings ApiSettings { get; set; } = new();

    /// <summary>
    /// 应用设置
    /// </summary>
    public AppSettings AppSettings { get; set; } = new();
}

/// <summary>
/// API 设置
/// </summary>
public class ApiSettings
{
    /// <summary>
    /// API 服务器基础地址
    /// </summary>
    public string BaseUrl { get; set; } = "http://101.201.49.208:6200";

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int Timeout { get; set; } = 30;
}

/// <summary>
/// 应用设置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 主题：Dark 或 Light
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// 语言：zh-CN 或 en-US
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// PDF报告生成路径（为空时使用应用程序根目录下的Reports文件夹）
    /// </summary>
    public string PdfReportPath { get; set; } = string.Empty;
}
