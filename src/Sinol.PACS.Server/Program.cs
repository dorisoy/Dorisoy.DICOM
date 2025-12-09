using FellowOakDicom;
using FellowOakDicom.Imaging;
using Sinol.PACS.Server.Models;
using Sinol.PACS.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel 监听所有接口（支持远程访问）
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5180); // HTTP
    // options.ListenAnyIP(5181, listenOptions => listenOptions.UseHttps()); // HTTPS（可选）
});

// 配置 DICOM 存储选项
builder.Services.Configure<DicomStorageOptions>(builder.Configuration.GetSection("DicomStorage"));

// 注册服务
builder.Services.AddSingleton<DicomIndexService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DicomIndexService>());
builder.Services.AddSingleton<DicomImageService>();

// 添加控制器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sinol PACS Server API",
        Version = "v1",
        Description = "DICOM 影像资料在线管理 API"
    });
});

// 配置 CORS 支持远程访问
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

// 初始化 fo-dicom
new DicomSetupBuilder()
    .RegisterServices(s => s.AddFellowOakDicom())
    .Build();

// 配置中间件
app.UseCors();

// 启用 Swagger（始终启用，便于调试和测试）
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sinol PACS Server API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.MapControllers();

// 添加根路径重定向到 Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// 输出启动信息
app.Logger.LogInformation("======================================");
app.Logger.LogInformation("Sinol PACS Server 启动中...");
app.Logger.LogInformation("API 文档: http://localhost:5180/swagger");
app.Logger.LogInformation("远程访问: http://<您的IP>:5180/swagger");
app.Logger.LogInformation("======================================");

app.Run();
