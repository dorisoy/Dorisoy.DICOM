namespace Sinol.PACS.Server.Models;

/// <summary>
/// DICOM 存储配置
/// </summary>
public class DicomStorageOptions
{
    public string RootPath { get; set; } = string.Empty;
    public string ThumbnailCachePath { get; set; } = "thumbnails";
    public int ThumbnailSize { get; set; } = 128;
}

/// <summary>
/// 患者信息
/// </summary>
public class PatientDto
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? BirthDate { get; set; }
    public string? Sex { get; set; }
    public int StudyCount { get; set; }
    public DateTime? LatestStudyDate { get; set; }
}

/// <summary>
/// 检查信息
/// </summary>
public class StudyDto
{
    public string StudyInstanceUid { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? StudyDate { get; set; }
    public string? StudyTime { get; set; }
    public string? StudyDescription { get; set; }
    public string? AccessionNumber { get; set; }
    public string? ReferringPhysician { get; set; }
    public string? Modalities { get; set; }
    public int SeriesCount { get; set; }
    public int InstanceCount { get; set; }
    public string? FolderPath { get; set; }
}

/// <summary>
/// 系列信息
/// </summary>
public class SeriesDto
{
    public string SeriesInstanceUid { get; set; } = string.Empty;
    public string StudyInstanceUid { get; set; } = string.Empty;
    public string? SeriesNumber { get; set; }
    public string? SeriesDescription { get; set; }
    public string? Modality { get; set; }
    public string? BodyPartExamined { get; set; }
    public int InstanceCount { get; set; }
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// 实例信息
/// </summary>
public class InstanceDto
{
    public string SopInstanceUid { get; set; } = string.Empty;
    public string SeriesInstanceUid { get; set; } = string.Empty;
    public string StudyInstanceUid { get; set; } = string.Empty;
    public int? InstanceNumber { get; set; }
    public string? SopClassUid { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int? Rows { get; set; }
    public int? Columns { get; set; }
    public int? NumberOfFrames { get; set; }
    public double? WindowCenter { get; set; }
    public double? WindowWidth { get; set; }
    public string? PhotometricInterpretation { get; set; }
}

/// <summary>
/// DICOM 标签信息
/// </summary>
public class DicomTagDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VR { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// 索引统计信息
/// </summary>
public class IndexStatistics
{
    public int TotalPatients { get; set; }
    public int TotalStudies { get; set; }
    public int TotalSeries { get; set; }
    public int TotalInstances { get; set; }
    public DateTime? LastIndexTime { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public bool IsIndexing { get; set; }
}

/// <summary>
/// API 响应包装
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };
    
    public static ApiResponse<T> Error(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// 分页响应
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// 查询参数
/// </summary>
public class QueryParameters
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
