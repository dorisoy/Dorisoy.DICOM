using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Sinol.PACS.Server.Models;

namespace Sinol.PACS.Server.Services;

/// <summary>
/// DICOM 图像处理服务 - 渲染、缩略图和格式转换
/// </summary>
public class DicomImageService
{
    private readonly ILogger<DicomImageService> _logger;
    private readonly DicomStorageOptions _options;
    private readonly DicomIndexService _indexService;
    private readonly string _thumbnailCachePath;

    public DicomImageService(
        ILogger<DicomImageService> logger,
        IOptions<DicomStorageOptions> options,
        DicomIndexService indexService)
    {
        _logger = logger;
        _options = options.Value;
        _indexService = indexService;
        
        // 设置缩略图缓存路径
        _thumbnailCachePath = Path.IsPathRooted(_options.ThumbnailCachePath)
            ? _options.ThumbnailCachePath
            : Path.Combine(AppContext.BaseDirectory, _options.ThumbnailCachePath);
        
        // 确保缓存目录存在
        if (!Directory.Exists(_thumbnailCachePath))
        {
            Directory.CreateDirectory(_thumbnailCachePath);
        }
    }

    /// <summary>
    /// 获取系列的缩略图
    /// </summary>
    public async Task<byte[]?> GetSeriesThumbnailAsync(string seriesInstanceUid, int size = 0)
    {
        if (size <= 0)
        {
            size = _options.ThumbnailSize;
        }

        // 检查缓存
        var cacheKey = $"{seriesInstanceUid}_{size}.jpg";
        var cachePath = Path.Combine(_thumbnailCachePath, cacheKey);
        
        if (File.Exists(cachePath))
        {
            return await File.ReadAllBytesAsync(cachePath);
        }

        // 获取系列的第一个实例
        var filePath = _indexService.GetFirstInstanceFilePath(seriesInstanceUid);
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        return await GenerateThumbnailAsync(filePath, cachePath, size);
    }

    /// <summary>
    /// 获取实例的缩略图
    /// </summary>
    public async Task<byte[]?> GetInstanceThumbnailAsync(string sopInstanceUid, int size = 0)
    {
        if (size <= 0)
        {
            size = _options.ThumbnailSize;
        }

        var filePath = _indexService.GetInstanceFilePath(sopInstanceUid);
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        // 检查缓存
        var cacheKey = $"inst_{sopInstanceUid}_{size}.jpg";
        var cachePath = Path.Combine(_thumbnailCachePath, cacheKey);
        
        if (File.Exists(cachePath))
        {
            return await File.ReadAllBytesAsync(cachePath);
        }

        return await GenerateThumbnailAsync(filePath, cachePath, size);
    }

    /// <summary>
    /// 获取渲染后的图像（JPEG格式）
    /// </summary>
    public async Task<byte[]?> GetRenderedImageAsync(
        string sopInstanceUid, 
        int frame = 0,
        double? windowCenter = null, 
        double? windowWidth = null,
        int quality = 85)
    {
        var filePath = _indexService.GetInstanceFilePath(sopInstanceUid);
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        try
        {
            var dicomFile = await DicomFile.OpenAsync(filePath);
            var dataset = dicomFile.Dataset;
            
            // 创建 DICOM 图像
            var dicomImage = new DicomImage(dataset, frame);
            
            // 设置窗宽窗位
            if (windowCenter.HasValue)
            {
                dicomImage.WindowCenter = windowCenter.Value;
            }
            if (windowWidth.HasValue)
            {
                dicomImage.WindowWidth = Math.Max(windowWidth.Value, 1);
            }

            // 渲染图像
            var renderedImage = dicomImage.RenderImage();
            var width = renderedImage.Width;
            var height = renderedImage.Height;
            
            // 获取像素数据
            var pixels = renderedImage.Pixels.Data;
            
            // 创建 ImageSharp 图像
            using var image = new Image<Rgba32>(width, height);
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var pixel = pixels[index];
                    
                    // ARGB 格式
                    var a = (byte)((pixel >> 24) & 0xFF);
                    var r = (byte)((pixel >> 16) & 0xFF);
                    var g = (byte)((pixel >> 8) & 0xFF);
                    var b = (byte)(pixel & 0xFF);
                    
                    image[x, y] = new Rgba32(r, g, b, a);
                }
            }

            // 编码为 JPEG
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = quality });
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "渲染图像失败: {SopInstanceUid}", sopInstanceUid);
            return null;
        }
    }

    /// <summary>
    /// 获取渲染后的图像（PNG格式）
    /// </summary>
    public async Task<byte[]?> GetRenderedImageAsPngAsync(
        string sopInstanceUid, 
        int frame = 0,
        double? windowCenter = null, 
        double? windowWidth = null)
    {
        var filePath = _indexService.GetInstanceFilePath(sopInstanceUid);
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        try
        {
            var dicomFile = await DicomFile.OpenAsync(filePath);
            var dataset = dicomFile.Dataset;
            
            var dicomImage = new DicomImage(dataset, frame);
            
            if (windowCenter.HasValue)
            {
                dicomImage.WindowCenter = windowCenter.Value;
            }
            if (windowWidth.HasValue)
            {
                dicomImage.WindowWidth = Math.Max(windowWidth.Value, 1);
            }

            var renderedImage = dicomImage.RenderImage();
            var width = renderedImage.Width;
            var height = renderedImage.Height;
            var pixels = renderedImage.Pixels.Data;
            
            using var image = new Image<Rgba32>(width, height);
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var pixel = pixels[index];
                    
                    var a = (byte)((pixel >> 24) & 0xFF);
                    var r = (byte)((pixel >> 16) & 0xFF);
                    var g = (byte)((pixel >> 8) & 0xFF);
                    var b = (byte)(pixel & 0xFF);
                    
                    image[x, y] = new Rgba32(r, g, b, a);
                }
            }

            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "渲染PNG图像失败: {SopInstanceUid}", sopInstanceUid);
            return null;
        }
    }

    /// <summary>
    /// 获取原始 DICOM 文件
    /// </summary>
    public async Task<byte[]?> GetDicomFileAsync(string sopInstanceUid)
    {
        var filePath = _indexService.GetInstanceFilePath(sopInstanceUid);
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    /// <summary>
    /// 获取帧数
    /// </summary>
    public async Task<int> GetFrameCountAsync(string sopInstanceUid)
    {
        var filePath = _indexService.GetInstanceFilePath(sopInstanceUid);
        if (string.IsNullOrEmpty(filePath))
        {
            return 0;
        }

        try
        {
            var dicomFile = await DicomFile.OpenAsync(filePath);
            var dataset = dicomFile.Dataset;
            
            if (!dataset.Contains(DicomTag.PixelData))
            {
                return 0;
            }

            var pixelData = DicomPixelData.Create(dataset);
            return Math.Max(1, pixelData.NumberOfFrames);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 生成缩略图
    /// </summary>
    private async Task<byte[]?> GenerateThumbnailAsync(string filePath, string cachePath, int size)
    {
        try
        {
            var dicomFile = await DicomFile.OpenAsync(filePath);
            var dataset = dicomFile.Dataset;
            
            // 检查是否有像素数据
            if (!dataset.Contains(DicomTag.PixelData))
            {
                return null;
            }

            // 渲染图像
            var dicomImage = new DicomImage(dataset, 0);
            var renderedImage = dicomImage.RenderImage();
            
            var width = renderedImage.Width;
            var height = renderedImage.Height;
            var pixels = renderedImage.Pixels.Data;
            
            // 创建 ImageSharp 图像
            using var image = new Image<Rgba32>(width, height);
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var pixel = pixels[index];
                    
                    var a = (byte)((pixel >> 24) & 0xFF);
                    var r = (byte)((pixel >> 16) & 0xFF);
                    var g = (byte)((pixel >> 8) & 0xFF);
                    var b = (byte)(pixel & 0xFF);
                    
                    image[x, y] = new Rgba32(r, g, b, a);
                }
            }

            // 调整大小（保持比例）
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Max
            }));

            // 保存到缓存并返回
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 80 });
            var bytes = ms.ToArray();

            // 异步保存到缓存（不阻塞返回）
            _ = Task.Run(async () =>
            {
                try
                {
                    await File.WriteAllBytesAsync(cachePath, bytes);
                }
                catch
                {
                    // 忽略缓存保存错误
                }
            });

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成缩略图失败: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// 清除缩略图缓存
    /// </summary>
    public void ClearThumbnailCache()
    {
        try
        {
            if (Directory.Exists(_thumbnailCachePath))
            {
                foreach (var file in Directory.GetFiles(_thumbnailCachePath))
                {
                    File.Delete(file);
                }
            }
            _logger.LogInformation("缩略图缓存已清除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除缩略图缓存失败");
        }
    }
}
