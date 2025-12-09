using FellowOakDicom;
using FellowOakDicom.Imaging;
using Sinol.DicomViewer.Core.Database;
using Sinol.DicomViewer.Core.Repositories;
using Sinol.DicomViewer.Core.Services;
using Sinol.DicomViewer.Models;
using Sinol.DicomViewer.Services;
using Sinol.DicomViewer.Views.Pages;
using Wpf.Ui.DependencyInjection;

namespace Sinol.DicomViewer;

/// <summary>
/// Sinol DICOM Viewer
/// </summary>
public partial class App
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            var basePath =
                Path.GetDirectoryName(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException(
                    "无法找到应用程序的基础目录。"
                );
            _ = c.SetBasePath(basePath);
        })
        .ConfigureServices(
            (context, services) =>
            {
                _ = services.AddNavigationViewPageProvider();

                // 应用程序宿主
                _ = services.AddHostedService<ApplicationHostService>();

                // 主题操作
                _ = services.AddSingleton<IThemeService, ThemeService>();

                // 任务栏操作
                _ = services.AddSingleton<ITaskBarService, TaskBarService>();

                // 包含导航的服务，与 INavigationWindow 相同...但没有窗口
                _ = services.AddSingleton<INavigationService, NavigationService>();

                // 对话框服务
                _ = services.AddSingleton<IContentDialogService, ContentDialogService>();

                // 配置服务 - 系统配置管理（必须在其他服务之前注册）
                _ = services.AddSingleton<IConfigService, ConfigService>();

                // 带导航的主窗口
                _ = services.AddSingleton<INavigationWindow, Views.MainWindow>();
                _ = services.AddSingleton(sp => new DicomLoader());
                _ = services.AddSingleton<PacsService>();
                _ = services.AddSingleton<PacsApiService>();
                // 数据库配置
                var dbOptions = GetDatabaseOptions(context.Configuration);
                _ = services.AddSingleton(dbOptions);
                _ = services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(sp.GetRequiredService<DatabaseOptions>()));
                _ = services.AddSingleton<IPatientRepository, PatientRepository>();
                _ = services.AddSingleton<IExaminationRepository, ExaminationRepository>();
                _ = services.AddSingleton<IReportRepository, ReportRepository>();
                _ = services.AddSingleton<PatientDbService>();
                _ = services.AddSingleton<ReportService>();
                _ = services.AddSingleton<MainViewModel>();

                _ = services.AddSingleton<MainPage>();

                // 配置
                _ = services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));

                // 自动注册所有其他页面和视图模型
                _ = services.AddTransientFromNamespace("Sinol.DicomViewer.Views", GalleryAssembly.Asssembly);
                _ = services.AddTransientFromNamespace("Sinol.DicomViewer.ViewModels", GalleryAssembly.Asssembly);
            }
        )
        .Build();

    /// <summary>
    /// 获取服务。
    /// </summary>
    public static IServiceProvider Services
    {
        get { return _host.Services; }
    }

    /// <summary>
    /// 应用程序加载时发生。
    /// </summary>
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        // 加载系统配置
        var configService = Services.GetRequiredService<IConfigService>();
        await configService.LoadConfigAsync();

        // 初始化 fo-dicom 图像管理器以支持各种传输语法（包括 JPEG 12位压缩格式）
        // fo-dicom.Codecs 包会自动注册原生转码器
        new DicomSetupBuilder()
            .RegisterServices(s => s
                .AddFellowOakDicom()
                .AddImageManager<WinFormsImageManager>())
            .Build();

        await _host.StartAsync();
    }

    /// <summary>
    /// 应用程序关闭时发生。
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    /// <summary>
    /// 当应用程序引发异常但未处理时发生。
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {

    }

    /// <summary>
    /// 从配置中获取数据库选项
    /// </summary>
    private static DatabaseOptions GetDatabaseOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Database");
        var options = new DatabaseOptions();

        // 读取数据库提供器
        var providerStr = section["Provider"] ?? "SQLite";
        options.Provider = providerStr.ToUpperInvariant() switch
        {
            "SQLITE" => DatabaseProvider.SQLite,
            "MYSQL" => DatabaseProvider.MySQL,
            "SQLSERVER" or "MSSQL" => DatabaseProvider.SqlServer,
            _ => DatabaseProvider.SQLite
        };

        // 读取其他配置
        options.ConnectionString = section["ConnectionString"] ?? string.Empty;
        options.SqliteFileName = section["SqliteFileName"] ?? "patients.db";
        
        if (bool.TryParse(section["AutoMigrate"], out var autoMigrate))
        {
            options.AutoMigrate = autoMigrate;
        }

        return options;
    }
}
