using Microsoft.AspNetCore.Mvc;
using Sinol.PACS.Server.Models;
using Sinol.PACS.Server.Services;

namespace Sinol.PACS.Server.Controllers;

/// <summary>
/// 系列管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SeriesController : ControllerBase
{
    private readonly DicomIndexService _indexService;
    private readonly DicomImageService _imageService;
    private readonly ILogger<SeriesController> _logger;

    public SeriesController(
        DicomIndexService indexService,
        DicomImageService imageService,
        ILogger<SeriesController> logger)
    {
        _indexService = indexService;
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系列详情
    /// </summary>
    [HttpGet("{seriesInstanceUid}")]
    public ActionResult<ApiResponse<SeriesDto>> GetSeries(string seriesInstanceUid)
    {
        var series = _indexService.GetSeries(seriesInstanceUid);
        if (series == null)
        {
            return NotFound(ApiResponse<SeriesDto>.Error("系列不存在"));
        }

        return Ok(ApiResponse<SeriesDto>.Ok(series));
    }

    /// <summary>
    /// 获取系列的实例列表
    /// </summary>
    [HttpGet("{seriesInstanceUid}/instances")]
    public ActionResult<ApiResponse<List<InstanceDto>>> GetSeriesInstances(string seriesInstanceUid)
    {
        var instances = _indexService.GetInstancesBySeries(seriesInstanceUid);
        return Ok(ApiResponse<List<InstanceDto>>.Ok(instances));
    }

    /// <summary>
    /// 获取系列缩略图
    /// </summary>
    [HttpGet("{seriesInstanceUid}/thumbnail")]
    public async Task<IActionResult> GetSeriesThumbnail(string seriesInstanceUid, [FromQuery] int size = 128)
    {
        var thumbnail = await _imageService.GetSeriesThumbnailAsync(seriesInstanceUid, size);
        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpeg");
    }
}

/// <summary>
/// 实例管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InstancesController : ControllerBase
{
    private readonly DicomIndexService _indexService;
    private readonly DicomImageService _imageService;
    private readonly ILogger<InstancesController> _logger;

    public InstancesController(
        DicomIndexService indexService,
        DicomImageService imageService,
        ILogger<InstancesController> logger)
    {
        _indexService = indexService;
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取实例详情
    /// </summary>
    [HttpGet("{sopInstanceUid}")]
    public ActionResult<ApiResponse<InstanceDto>> GetInstance(string sopInstanceUid)
    {
        var instance = _indexService.GetInstance(sopInstanceUid);
        if (instance == null)
        {
            return NotFound(ApiResponse<InstanceDto>.Error("实例不存在"));
        }

        return Ok(ApiResponse<InstanceDto>.Ok(instance));
    }

    /// <summary>
    /// 获取实例的 DICOM 标签
    /// </summary>
    [HttpGet("{sopInstanceUid}/tags")]
    public async Task<ActionResult<ApiResponse<List<DicomTagDto>>>> GetInstanceTags(string sopInstanceUid)
    {
        var tags = await _indexService.GetDicomTagsAsync(sopInstanceUid);
        return Ok(ApiResponse<List<DicomTagDto>>.Ok(tags));
    }

    /// <summary>
    /// 获取实例缩略图
    /// </summary>
    [HttpGet("{sopInstanceUid}/thumbnail")]
    public async Task<IActionResult> GetInstanceThumbnail(string sopInstanceUid, [FromQuery] int size = 128)
    {
        var thumbnail = await _imageService.GetInstanceThumbnailAsync(sopInstanceUid, size);
        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpeg");
    }

    /// <summary>
    /// 获取实例帧数
    /// </summary>
    [HttpGet("{sopInstanceUid}/frames")]
    public async Task<ActionResult<ApiResponse<int>>> GetFrameCount(string sopInstanceUid)
    {
        var count = await _imageService.GetFrameCountAsync(sopInstanceUid);
        return Ok(ApiResponse<int>.Ok(count));
    }
}
