using Sinol.DicomViewer.Models;

namespace Sinol.DicomViewer.Services;

/// <summary>
/// 配置服务接口 - 管理系统配置
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 当前系统配置
    /// </summary>
    SystemConfig Config { get; }

    /// <summary>
    /// 配置变更事件
    /// </summary>
    event EventHandler<SystemConfig>? ConfigChanged;

    /// <summary>
    /// 加载配置
    /// </summary>
    Task LoadConfigAsync();

    /// <summary>
    /// 保存配置
    /// </summary>
    Task SaveConfigAsync();

    /// <summary>
    /// 更新 API 服务器地址
    /// </summary>
    Task UpdateApiBaseUrlAsync(string baseUrl);

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    Task ResetToDefaultAsync();
}
