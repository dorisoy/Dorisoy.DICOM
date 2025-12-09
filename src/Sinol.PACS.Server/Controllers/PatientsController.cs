using Microsoft.AspNetCore.Mvc;
using Sinol.PACS.Server.Models;
using Sinol.PACS.Server.Services;

namespace Sinol.PACS.Server.Controllers;

/// <summary>
/// 患者管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly DicomIndexService _indexService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(DicomIndexService indexService, ILogger<PatientsController> logger)
    {
        _indexService = indexService;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者列表
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse<PagedResponse<PatientDto>>> GetPatients(
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

        var result = _indexService.GetPatients(query);
        return Ok(ApiResponse<PagedResponse<PatientDto>>.Ok(result));
    }

    /// <summary>
    /// 获取患者的检查列表
    /// </summary>
    [HttpGet("{patientId}/studies")]
    public ActionResult<ApiResponse<List<StudyDto>>> GetPatientStudies(string patientId)
    {
        var studies = _indexService.GetStudiesByPatient(patientId);
        return Ok(ApiResponse<List<StudyDto>>.Ok(studies));
    }
}
