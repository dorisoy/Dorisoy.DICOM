using Microsoft.AspNetCore.Mvc;
using Sinol.PACS.Server.Models;
using Sinol.PACS.Server.Services;

namespace Sinol.PACS.Server.Controllers;

/// <summary>
/// 检查管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StudiesController : ControllerBase
{
    private readonly DicomIndexService _indexService;
    private readonly ILogger<StudiesController> _logger;

    public StudiesController(DicomIndexService indexService, ILogger<StudiesController> logger)
    {
        _indexService = indexService;
        _logger = logger;
    }

    /// <summary>
    /// 获取检查列表
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse<PagedResponse<StudyDto>>> GetStudies(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = true)
    {
        var query = new QueryParameters
        {
            PageIndex = pageIndex,
            PageSize = Math.Clamp(pageSize, 1, 100),
            SearchTerm = search,
            SortBy = sortBy,
            SortDescending = sortDesc
        };

        var result = _indexService.GetStudies(query);
        return Ok(ApiResponse<PagedResponse<StudyDto>>.Ok(result));
    }

    /// <summary>
    /// 获取检查详情
    /// </summary>
    [HttpGet("{studyInstanceUid}")]
    public ActionResult<ApiResponse<StudyDto>> GetStudy(string studyInstanceUid)
    {
        var study = _indexService.GetStudy(studyInstanceUid);
        if (study == null)
        {
            return NotFound(ApiResponse<StudyDto>.Error("检查不存在"));
        }

        return Ok(ApiResponse<StudyDto>.Ok(study));
    }

    /// <summary>
    /// 获取检查的系列列表
    /// </summary>
    [HttpGet("{studyInstanceUid}/series")]
    public ActionResult<ApiResponse<List<SeriesDto>>> GetStudySeries(string studyInstanceUid)
    {
        var series = _indexService.GetSeriesByStudy(studyInstanceUid);
        return Ok(ApiResponse<List<SeriesDto>>.Ok(series));
    }
}
