using Microsoft.AspNetCore.Mvc;
using Sinol.PACS.Server.Models;
using Sinol.PACS.Server.Services;

namespace Sinol.PACS.Server.Controllers;

/// <summary>
/// WADO (Web Access to DICOM Objects) 风格的图像访问 API
/// 支持多种格式输出：DICOM、JPEG、PNG
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WadoController : ControllerBase
{
    private readonly DicomIndexService _indexService;
    private readonly DicomImageService _imageService;
    private readonly ILogger<WadoController> _logger;

    public WadoController(
        DicomIndexService indexService,
        DicomImageService imageService,
        ILogger<WadoController> logger)
    {
        _indexService = indexService;
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取 DICOM 对象（WADO-RS 风格）
    /// GET /api/wado/studies/{studyUID}/series/{seriesUID}/instances/{instanceUID}
    /// </summary>
    [HttpGet("studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}")]
    public async Task<IActionResult> GetDicomObject(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        [FromQuery] string? contentType = null)
    {
        // 验证实例存在
        var instance = _indexService.GetInstance(sopInstanceUid);
        if (instance == null)
        {
            return NotFound("实例不存在");
        }

        // 验证层级关系
        if (instance.StudyInstanceUid != studyInstanceUid || instance.SeriesInstanceUid != seriesInstanceUid)
        {
            return BadRequest("实例 UID 与检查/系列不匹配");
        }

        // 根据请求的内容类型返回不同格式
        var requestedType = contentType?.ToLowerInvariant() ?? Request.Headers.Accept.FirstOrDefault()?.ToLowerInvariant() ?? "application/dicom";

        if (requestedType.Contains("image/jpeg"))
        {
            return await GetInstanceAsJpeg(sopInstanceUid);
        }
        
        if (requestedType.Contains("image/png"))
        {
            return await GetInstanceAsPng(sopInstanceUid);
        }

        // 默认返回 DICOM 文件
        return await GetInstanceAsDicom(sopInstanceUid);
    }

    /// <summary>
    /// 获取渲染后的图像（JPEG格式）
    /// </summary>
    [HttpGet("image/{sopInstanceUid}")]
    public async Task<IActionResult> GetRenderedImage(
        string sopInstanceUid,
        [FromQuery] int frame = 0,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null,
        [FromQuery] int quality = 85)
    {
        var imageData = await _imageService.GetRenderedImageAsync(sopInstanceUid, frame, windowCenter, windowWidth, quality);
        if (imageData == null)
        {
            return NotFound("无法渲染图像");
        }

        return File(imageData, "image/jpeg");
    }

    /// <summary>
    /// 获取渲染后的图像（PNG格式）
    /// </summary>
    [HttpGet("image/{sopInstanceUid}/png")]
    public async Task<IActionResult> GetRenderedImagePng(
        string sopInstanceUid,
        [FromQuery] int frame = 0,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null)
    {
        var imageData = await _imageService.GetRenderedImageAsPngAsync(sopInstanceUid, frame, windowCenter, windowWidth);
        if (imageData == null)
        {
            return NotFound("无法渲染图像");
        }

        return File(imageData, "image/png");
    }

    /// <summary>
    /// 获取指定帧的图像
    /// </summary>
    [HttpGet("frames/{sopInstanceUid}/{frame}")]
    public async Task<IActionResult> GetFrame(
        string sopInstanceUid,
        int frame,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null,
        [FromQuery] int quality = 85)
    {
        var imageData = await _imageService.GetRenderedImageAsync(sopInstanceUid, frame, windowCenter, windowWidth, quality);
        if (imageData == null)
        {
            return NotFound("无法渲染帧");
        }

        return File(imageData, "image/jpeg");
    }

    /// <summary>
    /// 获取系列缩略图
    /// </summary>
    [HttpGet("thumbnail/{seriesInstanceUid}")]
    public async Task<IActionResult> GetSeriesThumbnail(string seriesInstanceUid, [FromQuery] int size = 128)
    {
        var thumbnail = await _imageService.GetSeriesThumbnailAsync(seriesInstanceUid, size);
        if (thumbnail == null)
        {
            return NotFound("无法生成缩略图");
        }

        return File(thumbnail, "image/jpeg");
    }

    /// <summary>
    /// 获取实例缩略图
    /// </summary>
    [HttpGet("thumbnail/instance/{sopInstanceUid}")]
    public async Task<IActionResult> GetInstanceThumbnail(string sopInstanceUid, [FromQuery] int size = 128)
    {
        var thumbnail = await _imageService.GetInstanceThumbnailAsync(sopInstanceUid, size);
        if (thumbnail == null)
        {
            return NotFound("无法生成缩略图");
        }

        return File(thumbnail, "image/jpeg");
    }

    /// <summary>
    /// 下载 DICOM 文件
    /// </summary>
    [HttpGet("dicom/{sopInstanceUid}")]
    public async Task<IActionResult> DownloadDicomFile(string sopInstanceUid)
    {
        return await GetInstanceAsDicom(sopInstanceUid, true);
    }

    /// <summary>
    /// 获取实例的 DICOM 标签
    /// </summary>
    [HttpGet("metadata/{sopInstanceUid}")]
    public async Task<ActionResult<ApiResponse<List<DicomTagDto>>>> GetMetadata(string sopInstanceUid)
    {
        var tags = await _indexService.GetDicomTagsAsync(sopInstanceUid);
        if (tags.Count == 0)
        {
            return NotFound(ApiResponse<List<DicomTagDto>>.Error("无法读取 DICOM 标签"));
        }

        return Ok(ApiResponse<List<DicomTagDto>>.Ok(tags));
    }

    #region 私有方法

    private async Task<IActionResult> GetInstanceAsDicom(string sopInstanceUid, bool asDownload = false)
    {
        var dicomData = await _imageService.GetDicomFileAsync(sopInstanceUid);
        if (dicomData == null)
        {
            return NotFound("DICOM 文件不存在");
        }

        if (asDownload)
        {
            return File(dicomData, "application/dicom", $"{sopInstanceUid}.dcm");
        }

        return File(dicomData, "application/dicom");
    }

    private async Task<IActionResult> GetInstanceAsJpeg(string sopInstanceUid)
    {
        var imageData = await _imageService.GetRenderedImageAsync(sopInstanceUid);
        if (imageData == null)
        {
            return NotFound("无法渲染图像");
        }

        return File(imageData, "image/jpeg");
    }

    private async Task<IActionResult> GetInstanceAsPng(string sopInstanceUid)
    {
        var imageData = await _imageService.GetRenderedImageAsPngAsync(sopInstanceUid);
        if (imageData == null)
        {
            return NotFound("无法渲染图像");
        }

        return File(imageData, "image/png");
    }

    #endregion
}

/// <summary>
/// 索引管理 API - 用于管理 DICOM 文件索引
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly DicomIndexService _indexService;
    private readonly DicomImageService _imageService;
    private readonly ILogger<IndexController> _logger;

    public IndexController(
        DicomIndexService indexService,
        DicomImageService imageService,
        ILogger<IndexController> logger)
    {
        _indexService = indexService;
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    [HttpGet("statistics")]
    public ActionResult<ApiResponse<IndexStatistics>> GetStatistics()
    {
        var stats = _indexService.GetStatistics();
        return Ok(ApiResponse<IndexStatistics>.Ok(stats));
    }

    /// <summary>
    /// 重建索引
    /// </summary>
    [HttpPost("rebuild")]
    public async Task<ActionResult<ApiResponse<IndexStatistics>>> RebuildIndex()
    {
        _logger.LogInformation("收到重建索引请求");
        
        var stats = await _indexService.RebuildIndexAsync();
        return Ok(ApiResponse<IndexStatistics>.Ok(stats, "索引重建完成"));
    }

    /// <summary>
    /// 清除缩略图缓存
    /// </summary>
    [HttpPost("clear-cache")]
    public ActionResult<ApiResponse<string>> ClearCache()
    {
        _imageService.ClearThumbnailCache();
        return Ok(ApiResponse<string>.Ok("缓存已清除"));
    }
}
