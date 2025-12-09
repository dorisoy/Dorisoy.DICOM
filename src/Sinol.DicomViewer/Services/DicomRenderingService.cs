using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sinol.DicomViewer.Core.Data;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System.Drawing;
using System.Drawing.Imaging;

namespace Sinol.DicomViewer.Services;

public sealed class DicomRenderingService
{
    public WriteableBitmap? Render(DicomFrame? frame, double windowCenter, double windowWidth)
    {
        if (frame is null)
        {
            return null;
        }

        try
        {
            var dicomImage = new DicomImage(frame.Dataset, frame.FrameIndex)
            {
                WindowCenter = windowCenter,
                WindowWidth = Math.Max(windowWidth, 1)
            };

            var renderedImage = dicomImage.RenderImage();
            
            // 在 fo-dicom 5.x 中，需要先转换为 Bitmap，再手动创建 WriteableBitmap
            using var bitmap = renderedImage.As<Bitmap>();
            
            var width = bitmap.Width;
            var height = bitmap.Height;
            
            // 创建 WriteableBitmap
            var writeableBitmap = new WriteableBitmap(
                width,
                height,
                96, // DPI X
                96, // DPI Y
                PixelFormats.Bgra32,
                null);
            
            // 锁定 bitmap 数据并复制到 WriteableBitmap
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            try
            {
                writeableBitmap.Lock();
                
                unsafe
                {
                    // 复制像素数据
                    byte* src = (byte*)bitmapData.Scan0;
                    byte* dst = (byte*)writeableBitmap.BackBuffer;
                    int bytes = Math.Abs(bitmapData.Stride) * height;
                    
                    for (int i = 0; i < bytes; i++)
                    {
                        dst[i] = src[i];
                    }
                }
                
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                writeableBitmap.Unlock();
                bitmap.UnlockBits(bitmapData);
            }
            
            writeableBitmap.Freeze();
            return writeableBitmap;
        }
        catch (NotSupportedException ex) when (ex.Message.Contains("transfer syntax") || ex.Message.Contains("Transfer Syntax") || ex.Message.Contains("Decoding"))
        {
            // 传输语法不支持的帧无法渲染，返回 null
            return null;
        }
        catch (Exception)
        {
            // 其他渲染错误也返回 null，避免程序崩溃
            return null;
        }
    }
}
