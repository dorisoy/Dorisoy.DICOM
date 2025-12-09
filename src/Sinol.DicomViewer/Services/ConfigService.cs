using Sinol.DicomViewer.Models;
using System.Text.Json;

namespace Sinol.DicomViewer.Services;

/// <summary>
/// 配置服务实现 - 管理系统配置
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _configFilePath;
    private SystemConfig _config;

    public ConfigService()
    {
        // 配置文件路径：应用程序目录下的 config.json
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _configFilePath = Path.Combine(appDirectory, "config.json");
        _config = new SystemConfig();
    }

    /// <inheritdoc/>
    public SystemConfig Config => _config;

    /// <inheritdoc/>
    public event EventHandler<SystemConfig>? ConfigChanged;

    /// <inheritdoc/>
    public async Task LoadConfigAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                var config = JsonSerializer.Deserialize<SystemConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config != null)
                {
                    _config = config;
                }
            }
            else
            {
                // 配置文件不存在，创建默认配置
                await SaveConfigAsync();
            }

            // 应用配置到 SdkClient
            ApplyConfigToSdkClient();
        }
        catch (Exception ex)
        {
            // 配置加载失败，使用默认配置
            System.Diagnostics.Debug.WriteLine($"配置加载失败: {ex.Message}");
            _config = new SystemConfig();
        }
    }

    /// <inheritdoc/>
    public async Task SaveConfigAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(_config, options);
            await File.WriteAllTextAsync(_configFilePath, json);

            // 应用配置到 SdkClient
            ApplyConfigToSdkClient();

            // 触发配置变更事件
            ConfigChanged?.Invoke(this, _config);
        }
        catch (Exception ex)
        {
            throw new Exception($"保存配置失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateApiBaseUrlAsync(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("API地址不能为空", nameof(baseUrl));
        }

        // 验证 URL 格式
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("API地址格式不正确", nameof(baseUrl));
        }

        _config.ApiSettings.BaseUrl = baseUrl.TrimEnd('/');
        await SaveConfigAsync();
    }

    /// <inheritdoc/>
    public async Task ResetToDefaultAsync()
    {
        _config = new SystemConfig();
        await SaveConfigAsync();
    }

    /// <summary>
    /// 应用配置到 SdkClient
    /// </summary>
    private void ApplyConfigToSdkClient()
    {
        // SdkClient 尚未实现，暂时跳过
        // TODO: 实现 SdkClient 配置后启用此代码
        // SdkClient.BaseURL = _config.ApiSettings.BaseUrl;
        // if (_config.ApiSettings.Timeout > 0)
        // {
        //     SdkClient.HttpClient.Timeout = TimeSpan.FromSeconds(_config.ApiSettings.Timeout);
        // }
    }
}
