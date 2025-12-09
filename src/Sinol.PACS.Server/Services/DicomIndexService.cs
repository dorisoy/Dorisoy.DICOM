using System.Collections.Concurrent;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.Options;
using Sinol.PACS.Server.Models;

namespace Sinol.PACS.Server.Services;

/// <summary>
/// DICOM 文件索引服务 - 扫描、解析和管理 DICOM 文件
/// </summary>
public class DicomIndexService : IHostedService
{
    private readonly ILogger<DicomIndexService> _logger;
    private readonly DicomStorageOptions _options;
    
    // 内存索引存储
    private readonly ConcurrentDictionary<string, PatientRecord> _patients = new();
    private readonly ConcurrentDictionary<string, StudyRecord> _studies = new();
    private readonly ConcurrentDictionary<string, SeriesRecord> _series = new();
    private readonly ConcurrentDictionary<string, InstanceRecord> _instances = new();
    
    private bool _isIndexing;
    private DateTime? _lastIndexTime;
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    public DicomIndexService(ILogger<DicomIndexService> logger, IOptions<DicomStorageOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DICOM 索引服务启动，存储路径: {Path}", _options.RootPath);
        
        // 启动时自动索引
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000, cancellationToken);
            await RebuildIndexAsync(cancellationToken);
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DICOM 索引服务停止");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 重建完整索引
    /// </summary>
    public async Task<IndexStatistics> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_isIndexing)
        {
            return GetStatistics();
        }

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            _isIndexing = true;
            _logger.LogInformation("开始重建 DICOM 索引...");

            // 清空现有索引
            _patients.Clear();
            _studies.Clear();
            _series.Clear();
            _instances.Clear();

            if (!Directory.Exists(_options.RootPath))
            {
                _logger.LogWarning("存储路径不存在: {Path}", _options.RootPath);
                return GetStatistics();
            }

            // 扫描所有文件
            var files = Directory.EnumerateFiles(_options.RootPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                           !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                           !f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("找到 {Count} 个文件待处理", files.Count);

            var processedCount = 0;
            var errorCount = 0;

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await IndexFileAsync(file);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogDebug("跳过文件 {File}: {Error}", file, ex.Message);
                }

                if (processedCount % 100 == 0)
                {
                    _logger.LogInformation("已处理 {Count}/{Total} 个文件", processedCount, files.Count);
                }
            }

            _lastIndexTime = DateTime.Now;
            _logger.LogInformation("索引完成: {Processed} 个文件成功, {Errors} 个错误", processedCount, errorCount);

            return GetStatistics();
        }
        finally
        {
            _isIndexing = false;
            _indexLock.Release();
        }
    }

    /// <summary>
    /// 索引单个 DICOM 文件
    /// </summary>
    private async Task IndexFileAsync(string filePath)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;

        // 获取关键标识符
        var studyUid = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
        var seriesUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
        var sopInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
        var patientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, "Unknown");

        if (string.IsNullOrEmpty(studyUid) || string.IsNullOrEmpty(seriesUid) || string.IsNullOrEmpty(sopInstanceUid))
        {
            return;
        }

        // 创建或更新患者记录
        var patientRecord = _patients.GetOrAdd(patientId, id => new PatientRecord
        {
            PatientId = id,
            PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, "Unknown")?.ToString() ?? "Unknown",
            BirthDate = dataset.GetSingleValueOrDefault<string?>(DicomTag.PatientBirthDate, null),
            Sex = dataset.GetSingleValueOrDefault<string?>(DicomTag.PatientSex, null)
        });

        // 创建或更新检查记录
        var studyRecord = _studies.GetOrAdd(studyUid, uid => new StudyRecord
        {
            StudyInstanceUid = uid,
            PatientId = patientId,
            PatientName = patientRecord.PatientName,
            StudyDate = dataset.GetSingleValueOrDefault<string?>(DicomTag.StudyDate, null),
            StudyTime = dataset.GetSingleValueOrDefault<string?>(DicomTag.StudyTime, null),
            StudyDescription = dataset.GetSingleValueOrDefault<string?>(DicomTag.StudyDescription, null),
            AccessionNumber = dataset.GetSingleValueOrDefault<string?>(DicomTag.AccessionNumber, null),
            ReferringPhysician = dataset.GetSingleValueOrDefault<string?>(DicomTag.ReferringPhysicianName, null),
            FolderPath = Path.GetDirectoryName(filePath)
        });

        // 更新检查的 Modalities
        var modality = dataset.GetSingleValueOrDefault<string?>(DicomTag.Modality, null);
        if (!string.IsNullOrEmpty(modality) && !studyRecord.Modalities.Contains(modality))
        {
            studyRecord.Modalities.Add(modality);
        }

        // 创建或更新系列记录
        var seriesRecord = _series.GetOrAdd(seriesUid, uid => new SeriesRecord
        {
            SeriesInstanceUid = uid,
            StudyInstanceUid = studyUid,
            SeriesNumber = dataset.GetSingleValueOrDefault<string?>(DicomTag.SeriesNumber, null),
            SeriesDescription = dataset.GetSingleValueOrDefault<string?>(DicomTag.SeriesDescription, null),
            Modality = modality,
            BodyPartExamined = dataset.GetSingleValueOrDefault<string?>(DicomTag.BodyPartExamined, null)
        });

        // 创建实例记录
        var instanceRecord = new InstanceRecord
        {
            SopInstanceUid = sopInstanceUid,
            SeriesInstanceUid = seriesUid,
            StudyInstanceUid = studyUid,
            InstanceNumber = dataset.GetSingleValueOrDefault<int?>(DicomTag.InstanceNumber, null),
            SopClassUid = dataset.GetSingleValueOrDefault<string?>(DicomTag.SOPClassUID, null),
            FilePath = filePath,
            Rows = dataset.GetSingleValueOrDefault<int?>(DicomTag.Rows, null),
            Columns = dataset.GetSingleValueOrDefault<int?>(DicomTag.Columns, null),
            NumberOfFrames = dataset.GetSingleValueOrDefault<int?>(DicomTag.NumberOfFrames, null),
            WindowCenter = dataset.GetSingleValueOrDefault<double?>(DicomTag.WindowCenter, null),
            WindowWidth = dataset.GetSingleValueOrDefault<double?>(DicomTag.WindowWidth, null),
            PhotometricInterpretation = dataset.GetSingleValueOrDefault<string?>(DicomTag.PhotometricInterpretation, null)
        };

        _instances[sopInstanceUid] = instanceRecord;

        // 更新计数
        patientRecord.StudyUids.Add(studyUid);
        studyRecord.SeriesUids.Add(seriesUid);
        seriesRecord.InstanceUids.Add(sopInstanceUid);

        // 更新最新检查日期
        if (DateTime.TryParseExact(studyRecord.StudyDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var studyDate))
        {
            if (!patientRecord.LatestStudyDate.HasValue || studyDate > patientRecord.LatestStudyDate)
            {
                patientRecord.LatestStudyDate = studyDate;
            }
        }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public IndexStatistics GetStatistics() => new()
    {
        TotalPatients = _patients.Count,
        TotalStudies = _studies.Count,
        TotalSeries = _series.Count,
        TotalInstances = _instances.Count,
        LastIndexTime = _lastIndexTime,
        StoragePath = _options.RootPath,
        IsIndexing = _isIndexing
    };

    /// <summary>
    /// 获取所有患者
    /// </summary>
    public PagedResponse<PatientDto> GetPatients(QueryParameters query)
    {
        var patients = _patients.Values.AsEnumerable();

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLowerInvariant();
            patients = patients.Where(p =>
                p.PatientId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // 排序
        patients = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? patients.OrderByDescending(p => p.PatientName)
                : patients.OrderBy(p => p.PatientName),
            "date" => query.SortDescending
                ? patients.OrderByDescending(p => p.LatestStudyDate)
                : patients.OrderBy(p => p.LatestStudyDate),
            _ => patients.OrderByDescending(p => p.LatestStudyDate)
        };

        var totalCount = patients.Count();
        var items = patients
            .Skip(query.PageIndex * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PatientDto
            {
                PatientId = p.PatientId,
                PatientName = p.PatientName,
                BirthDate = p.BirthDate,
                Sex = p.Sex,
                StudyCount = p.StudyUids.Count,
                LatestStudyDate = p.LatestStudyDate
            })
            .ToList();

        return new PagedResponse<PatientDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// 获取患者的检查列表
    /// </summary>
    public List<StudyDto> GetStudiesByPatient(string patientId)
    {
        if (!_patients.TryGetValue(patientId, out var patient))
        {
            return [];
        }

        return patient.StudyUids
            .Where(uid => _studies.TryGetValue(uid, out _))
            .Select(uid => _studies[uid])
            .Select(ToStudyDto)
            .OrderByDescending(s => s.StudyDate)
            .ToList();
    }

    /// <summary>
    /// 获取所有检查
    /// </summary>
    public PagedResponse<StudyDto> GetStudies(QueryParameters query)
    {
        var studies = _studies.Values.AsEnumerable();

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLowerInvariant();
            studies = studies.Where(s =>
                s.PatientId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.StudyDescription?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.AccessionNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // 排序
        studies = query.SortBy?.ToLowerInvariant() switch
        {
            "patient" => query.SortDescending
                ? studies.OrderByDescending(s => s.PatientName)
                : studies.OrderBy(s => s.PatientName),
            "description" => query.SortDescending
                ? studies.OrderByDescending(s => s.StudyDescription)
                : studies.OrderBy(s => s.StudyDescription),
            _ => query.SortDescending
                ? studies.OrderByDescending(s => s.StudyDate)
                : studies.OrderBy(s => s.StudyDate)
        };

        var totalCount = studies.Count();
        var items = studies
            .Skip(query.PageIndex * query.PageSize)
            .Take(query.PageSize)
            .Select(ToStudyDto)
            .ToList();

        return new PagedResponse<StudyDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// 获取检查详情
    /// </summary>
    public StudyDto? GetStudy(string studyInstanceUid)
    {
        return _studies.TryGetValue(studyInstanceUid, out var study) ? ToStudyDto(study) : null;
    }

    /// <summary>
    /// 获取检查的系列列表
    /// </summary>
    public List<SeriesDto> GetSeriesByStudy(string studyInstanceUid)
    {
        if (!_studies.TryGetValue(studyInstanceUid, out var study))
        {
            return [];
        }

        return study.SeriesUids
            .Where(uid => _series.TryGetValue(uid, out _))
            .Select(uid => _series[uid])
            .Select(ToSeriesDto)
            .OrderBy(s => s.SeriesNumber)
            .ToList();
    }

    /// <summary>
    /// 获取系列详情
    /// </summary>
    public SeriesDto? GetSeries(string seriesInstanceUid)
    {
        return _series.TryGetValue(seriesInstanceUid, out var series) ? ToSeriesDto(series) : null;
    }

    /// <summary>
    /// 获取系列的实例列表
    /// </summary>
    public List<InstanceDto> GetInstancesBySeries(string seriesInstanceUid)
    {
        if (!_series.TryGetValue(seriesInstanceUid, out var series))
        {
            return [];
        }

        return series.InstanceUids
            .Where(uid => _instances.TryGetValue(uid, out _))
            .Select(uid => _instances[uid])
            .Select(ToInstanceDto)
            .OrderBy(i => i.InstanceNumber)
            .ToList();
    }

    /// <summary>
    /// 获取实例详情
    /// </summary>
    public InstanceDto? GetInstance(string sopInstanceUid)
    {
        return _instances.TryGetValue(sopInstanceUid, out var instance) ? ToInstanceDto(instance) : null;
    }

    /// <summary>
    /// 获取实例文件路径
    /// </summary>
    public string? GetInstanceFilePath(string sopInstanceUid)
    {
        return _instances.TryGetValue(sopInstanceUid, out var instance) ? instance.FilePath : null;
    }

    /// <summary>
    /// 获取系列的第一个实例文件路径（用于缩略图）
    /// </summary>
    public string? GetFirstInstanceFilePath(string seriesInstanceUid)
    {
        if (!_series.TryGetValue(seriesInstanceUid, out var series))
        {
            return null;
        }

        var firstInstanceUid = series.InstanceUids.FirstOrDefault();
        if (firstInstanceUid == null)
        {
            return null;
        }

        return _instances.TryGetValue(firstInstanceUid, out var instance) ? instance.FilePath : null;
    }

    /// <summary>
    /// 获取 DICOM 文件的所有标签
    /// </summary>
    public async Task<List<DicomTagDto>> GetDicomTagsAsync(string sopInstanceUid)
    {
        var filePath = GetInstanceFilePath(sopInstanceUid);
        if (string.IsNullOrEmpty(filePath))
        {
            return [];
        }

        try
        {
            var dicomFile = await DicomFile.OpenAsync(filePath);
            var tags = new List<DicomTagDto>();

            foreach (var item in dicomFile.Dataset)
            {
                try
                {
                    var tag = item.Tag;
                    var name = tag.DictionaryEntry?.Name ?? "Unknown";
                    var vr = item.ValueRepresentation.Code;
                    var value = GetTagValue(dicomFile.Dataset, item);

                    tags.Add(new DicomTagDto
                    {
                        Tag = $"({tag.Group:X4},{tag.Element:X4})",
                        Name = name,
                        VR = vr,
                        Value = value
                    });
                }
                catch
                {
                    // 忽略无法读取的标签
                }
            }

            return tags;
        }
        catch
        {
            return [];
        }
    }

    private static string GetTagValue(DicomDataset dataset, DicomItem item)
    {
        try
        {
            if (item is DicomSequence)
            {
                return "[Sequence]";
            }

            if (item.Tag == DicomTag.PixelData)
            {
                return "[Pixel Data]";
            }

            var values = dataset.GetValueCount(item.Tag) > 0
                ? dataset.GetString(item.Tag)
                : string.Empty;

            // 截断过长的值
            if (values.Length > 100)
            {
                values = values[..100] + "...";
            }

            return values;
        }
        catch
        {
            return string.Empty;
        }
    }

    private StudyDto ToStudyDto(StudyRecord record) => new()
    {
        StudyInstanceUid = record.StudyInstanceUid,
        PatientId = record.PatientId,
        PatientName = record.PatientName,
        StudyDate = record.StudyDate,
        StudyTime = record.StudyTime,
        StudyDescription = record.StudyDescription,
        AccessionNumber = record.AccessionNumber,
        ReferringPhysician = record.ReferringPhysician,
        Modalities = string.Join(", ", record.Modalities),
        SeriesCount = record.SeriesUids.Count,
        InstanceCount = record.SeriesUids.Sum(uid => _series.TryGetValue(uid, out var s) ? s.InstanceUids.Count : 0),
        FolderPath = record.FolderPath
    };

    private SeriesDto ToSeriesDto(SeriesRecord record) => new()
    {
        SeriesInstanceUid = record.SeriesInstanceUid,
        StudyInstanceUid = record.StudyInstanceUid,
        SeriesNumber = record.SeriesNumber,
        SeriesDescription = record.SeriesDescription,
        Modality = record.Modality,
        BodyPartExamined = record.BodyPartExamined,
        InstanceCount = record.InstanceUids.Count,
        ThumbnailUrl = $"/api/wado/thumbnail/{record.SeriesInstanceUid}"
    };

    private static InstanceDto ToInstanceDto(InstanceRecord record) => new()
    {
        SopInstanceUid = record.SopInstanceUid,
        SeriesInstanceUid = record.SeriesInstanceUid,
        StudyInstanceUid = record.StudyInstanceUid,
        InstanceNumber = record.InstanceNumber,
        SopClassUid = record.SopClassUid,
        FilePath = record.FilePath,
        Rows = record.Rows,
        Columns = record.Columns,
        NumberOfFrames = record.NumberOfFrames,
        WindowCenter = record.WindowCenter,
        WindowWidth = record.WindowWidth,
        PhotometricInterpretation = record.PhotometricInterpretation
    };

    #region 内部记录类型

    private class PatientRecord
    {
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? BirthDate { get; set; }
        public string? Sex { get; set; }
        public HashSet<string> StudyUids { get; } = [];
        public DateTime? LatestStudyDate { get; set; }
    }

    private class StudyRecord
    {
        public string StudyInstanceUid { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? StudyDate { get; set; }
        public string? StudyTime { get; set; }
        public string? StudyDescription { get; set; }
        public string? AccessionNumber { get; set; }
        public string? ReferringPhysician { get; set; }
        public HashSet<string> Modalities { get; } = [];
        public HashSet<string> SeriesUids { get; } = [];
        public string? FolderPath { get; set; }
    }

    private class SeriesRecord
    {
        public string SeriesInstanceUid { get; set; } = string.Empty;
        public string StudyInstanceUid { get; set; } = string.Empty;
        public string? SeriesNumber { get; set; }
        public string? SeriesDescription { get; set; }
        public string? Modality { get; set; }
        public string? BodyPartExamined { get; set; }
        public HashSet<string> InstanceUids { get; } = [];
    }

    private class InstanceRecord
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

    #endregion
}
