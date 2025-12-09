namespace Sinol.DicomViewer.Models;

/// <summary>
/// 窗位窗宽预设
/// </summary>
public record WindowPreset(string Name, double WindowCenter, double WindowWidth)
{
    /// <summary>
    /// CT 常用预设
    /// </summary>
    public static readonly WindowPreset[] CtPresets =
    {
        new("窗口预设", 40, 400),
        new("肺窗", -600, 1500),
        new("纵隔窗", 40, 400),
        new("骨窗", 300, 1500),
        new("脑窗", 40, 80),
        new("脑卒中窗", 32, 8),
        new("肝窗", 60, 150),
        new("腹部窗", 40, 350),
        new("脊柱窗", 50, 350),
        new("软组织窗", 50, 400),
    };

    /// <summary>
    /// MR 常用预设
    /// </summary>
    public static readonly WindowPreset[] MrPresets =
    {
        new("默认", 500, 1000),
        new("T1", 500, 1000),
        new("T2", 400, 800),
        new("FLAIR", 600, 1200),
    };

    /// <summary>
    /// X光/DR 常用预设
    /// </summary>
    public static readonly WindowPreset[] XrayPresets =
    {
        new("默认", 2048, 4096),
        new("胸片", 500, 2000),
        new("骨骼", 300, 1500),
    };
}
