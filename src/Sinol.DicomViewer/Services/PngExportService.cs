using System.IO;
using System.Windows.Media.Imaging;

namespace Sinol.DicomViewer.Services;

public sealed class PngExportService
{
    public void Export(string path, BitmapSource bitmap)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
