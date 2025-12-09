using System.Windows;
using System.Windows.Controls;

namespace Sinol.DicomViewer.Helpers;

/// <summary>
/// PasswordBox 绑定辅助类
/// </summary>
public static class PasswordBoxHelper
{
    /// <summary>
    /// 绑定密码依赖属性
    /// </summary>
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged)
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            });

    /// <summary>
    /// 绑定密码更新依赖属性（内部使用）
    /// </summary>
    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached(
            "UpdatingPassword",
            typeof(bool),
            typeof(PasswordBoxHelper));

    /// <summary>
    /// 获取绑定的密码
    /// </summary>
    public static string GetBoundPassword(DependencyObject d)
    {
        return (string)d.GetValue(BoundPasswordProperty);
    }

    /// <summary>
    /// 设置绑定的密码
    /// </summary>
    public static void SetBoundPassword(DependencyObject d, string value)
    {
        d.SetValue(BoundPasswordProperty, value);
    }

    /// <summary>
    /// 获取密码更新标志
    /// </summary>
    private static bool GetUpdatingPassword(DependencyObject d)
    {
        return (bool)d.GetValue(UpdatingPasswordProperty);
    }

    /// <summary>
    /// 设置密码更新标志
    /// </summary>
    private static void SetUpdatingPassword(DependencyObject d, bool value)
    {
        d.SetValue(UpdatingPasswordProperty, value);
    }

    /// <summary>
    /// 绑定密码变更处理
    /// </summary>
    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
            return;

        // 移除旧的事件处理器
        passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

        // 如果不是正在更新密码（避免循环更新）
        if (!GetUpdatingPassword(passwordBox))
        {
            passwordBox.Password = e.NewValue as string ?? string.Empty;
        }

        // 添加新的事件处理器
        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
    }

    /// <summary>
    /// PasswordBox 密码变更事件处理
    /// </summary>
    private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        // 设置更新标志，避免循环更新
        SetUpdatingPassword(passwordBox, true);

        // 更新绑定的属性
        SetBoundPassword(passwordBox, passwordBox.Password);

        // 重置更新标志
        SetUpdatingPassword(passwordBox, false);
    }
}
