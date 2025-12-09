using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Sinol.DicomViewer.Core.Services;
using Sinol.DicomViewer.Services;
using Sinol.DicomViewer.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Sinol.DicomViewer.Views;

public partial class MainWindow : INavigationWindow
{
    public ViewModels.MainWindowViewModel ViewModel { get; }
    private readonly IContentDialogService _contentDialogService;
    private readonly PacsService _pacsService;
    private readonly PacsApiService _pacsApiService;
    private readonly DicomLoader _dicomLoader;

    public MainWindow(ViewModels.MainWindowViewModel viewModel,
                      INavigationService navigationService,
                      IContentDialogService contentDialogService,
                      PacsService pacsService,
                      PacsApiService pacsApiService,
                      DicomLoader dicomLoader)
    {
        ViewModel = viewModel;
        _contentDialogService = contentDialogService;
        _pacsService = pacsService;
        _pacsApiService = pacsApiService;
        _dicomLoader = dicomLoader;
        DataContext = this;

        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
        contentDialogService.SetDialogHost(RootContentDialog);
    }

    /// <summary>
    /// 切换侧栏显示/隐藏
    /// </summary>
    private void OnToggleSidebar(object sender, RoutedEventArgs e)
    {
        // 通过导航视图的内容获取当前页面
        var frame = FindVisualChild<Frame>(RootNavigation);
        if (frame?.Content is MainPage mainPage)
        {
            mainPage.ToggleSidebar();
            // 更新菜单文本
            if (sender is System.Windows.Controls.MenuItem menuItem)
            {
                menuItem.Header = mainPage.IsSidebarVisible ? "隐藏侧栏" : "显示侧栏";
            }
        }
    }

    /// <summary>
    /// 查找视觉树中的子元素
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }
            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// 显示操作说明对话框
    /// </summary>
    private async void OnShowHelpGuide(object sender, RoutedEventArgs e)
    {
        var content = CreateHelpGuideContent();
        
        await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "操作指南",
                Content = content,
                CloseButtonText = "关闭"
            });
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    private async void OnCheckUpdate(object sender, RoutedEventArgs e)
    {
        var content = CreateUpdateContent();
        
        await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "检查更新",
                Content = content,
                PrimaryButtonText = "立即更新",
                CloseButtonText = "稍后提醒"
            });
    }

    /// <summary>
    /// 创建更新内容
    /// </summary>
    private static StackPanel CreateUpdateContent()
    {
        var panel = new StackPanel { Width = 350 };
        
        // 当前版本
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "当前版本: v1.0.0",
            FontSize = 18,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cccccc")),
            Margin = new Thickness(0, 0, 0, 8)
        });
        
        // 最新版本
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "最新版本: v1.1.0",
            FontSize = 18,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50")),
            Margin = new Thickness(0, 0, 0, 16)
        });
        
        // 更新内容标题
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "更新内容:",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f1f1f1")),
            Margin = new Thickness(0, 0, 0, 8)
        });
        
        // 更新列表
        var updateItems = new[]
        {
            "• 新增 MPR 多平面重建功能",
            "• 优化窗宽窗位调整体验",
            "• 支持更多 DICOM 传输语法",
            "• 修复已知问题并提升稳定性"
        };
        
        foreach (var item in updateItems)
        {
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = item,
                FontSize = 12,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cccccc")),
                Margin = new Thickness(8, 2, 0, 2),
                TextWrapping = TextWrapping.Wrap
            });
        }
        
        return panel;
    }

    /// <summary>
    /// 创建操作说明内容
    /// </summary>
    private static StackPanel CreateHelpGuideContent()
    {
        var panel = new StackPanel { Width = 400 };

        // 基本操作部分
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "基本操作",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f1f1f1")),
            Margin = new Thickness(0, 0, 0, 8)
        });

        AddHelpItem(panel, "左键:", "点击定位 / 调整窗宽窗位");
        AddHelpItem(panel, "右键:", "平移图像");
        AddHelpItem(panel, "滚轮:", "切换切片");
        AddHelpItem(panel, "Ctrl+滚轮:", "缩放图像");

        // 测量工具部分
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "测量工具",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f1f1f1")),
            Margin = new Thickness(0, 16, 0, 8)
        });

        AddHelpItem(panel, "距离测量:", "选择工具后，点击两点定义距离");
        AddHelpItem(panel, "角度测量:", "选择工具后，点击三点定义角度");
        AddHelpItem(panel, "ROI分析:", "选择矩形/椭圆工具，拖动绘制区域");

        // 窗宽窗位部分
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "窗宽窗位调整",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f1f1f1")),
            Margin = new Thickness(0, 16, 0, 8)
        });

        AddHelpItem(panel, "左右拖动:", "调整窗宽 (Window Width)");
        AddHelpItem(panel, "上下拖动:", "调整窗位 (Window Level)");
        AddHelpItem(panel, "预设选择:", "使用工具栏预设下拉框快速设置");

        // 视图操作部分
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "视图操作",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f1f1f1")),
            Margin = new Thickness(0, 16, 0, 8)
        });

        AddHelpItem(panel, "最大化:", "点击视图右上角按钮最大化单个视图");
        AddHelpItem(panel, "十字线联动:", "点击任意视图同步定位其他视图");
        AddHelpItem(panel, "Cine播放:", "开启后自动循环播放序列帧");

        return panel;
    }

    /// <summary>
    /// 添加帮助项目
    /// </summary>
    private static void AddHelpItem(StackPanel parent, string label, string description)
    {
        var textBlock = new System.Windows.Controls.TextBlock
        {
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cccccc")),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };

        textBlock.Inlines.Add(new Run(label) { FontWeight = FontWeights.SemiBold });
        textBlock.Inlines.Add(new Run(" " + description));

        parent.Children.Add(textBlock);
    }

    /// <summary>
    /// 打开PACS查询窗口
    /// </summary>
    private async void OnOpenPacsQuery(object sender, RoutedEventArgs e)
    {
        var pacsWindow = new PacsQueryWindow(_pacsService, _pacsApiService, _dicomLoader)
        {
            Owner = this
        };
        
        if (pacsWindow.ShowDialog() == true && !string.IsNullOrEmpty(pacsWindow.DownloadedStudyPath))
        {
            // 下载完成，加载到主界面
            var frame = FindVisualChild<Frame>(RootNavigation);
            if (frame?.Content is MainPage mainPage)
            {
                await mainPage.LoadFromFolderAsync(pacsWindow.DownloadedStudyPath);
            }
        }
    }
    
    /// <summary>
    /// 上传到PACS
    /// </summary>
    private async void OnUploadToPacs(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择要上传的DICOM文件",
            Filter = "DICOM文件|*.dcm;*.DCM|所有文件|*.*",
            Multiselect = true
        };
        
        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
        {
            return;
        }
        
        // 显示服务器选择对话框
        var content = new StackPanel { Width = 300 };
        content.Children.Add(new System.Windows.Controls.TextBlock 
        { 
            Text = $"已选择 {dialog.FileNames.Length} 个文件",
            Margin = new Thickness(0, 0, 0, 16)
        });
        content.Children.Add(new System.Windows.Controls.TextBlock 
        { 
            Text = "请在PACS服务器管理中配置目标服务器，然后再进行上传。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#a0a0a0"))
        });
        
        await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "上传到PACS",
                Content = content,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消"
            });
    }
    
    /// <summary>
    /// 管理PACS服务器 - 导航到设置页面
    /// </summary>
    private void OnManagePacsServers(object sender, RoutedEventArgs e)
    {
        // 导航到设置页面
        RootNavigation.Navigate(typeof(SettingsPage));
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
        RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    /// <summary>
    /// 主窗体关闭时退出应用程序（除非是注销操作）
    /// </summary>
    /// <param name="e"></param>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        System.Windows.Application.Current.Shutdown();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider) { }
}
