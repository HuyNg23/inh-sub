using IBox.Common;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Database.DBC;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx;
using IBox.MailService;
using IBox.PageBuilder;
using IBox.PageExecute;
using IBox.Permissions;
using IBox.Schedule.Library;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Workflow;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<Configuration>(builder.Configuration.GetSection("IBoxConfig"));
builder.Services.AddServiceCommon();
builder.Services.AddServiceRootDB();
builder.Services.AddServiceTenantDB();
builder.Services.AddServiceWorkflow();
builder.Services.AddServiceDLLEx();
builder.Services.AddServiceDBC();
builder.Services.AddServiceMail();
builder.Services.AddServicePageExecute();
builder.Services.AddServicePageBuilder();
builder.Services.AddServicePermissionsTenant();
builder.Services.AddServiceScheduleLibrary();
builder.Services.AddServiceGetDataLog();

builder.Services.AddControllers()
           .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.WebHost.ConfigureKestrel(options =>
{
    var kestrelConfig = builder.Configuration.GetSection("Kestrel");
    var limits = kestrelConfig.GetSection("Limits");
    if (limits.Exists())
    {
        options.Limits.MaxConcurrentConnections = limits.GetValue<int?>("MaxConcurrentConnections"); // Số kết nối đồng thời tối đa
        options.Limits.MaxConcurrentUpgradedConnections = 100; // Số kết nối tối đa sử dụng WebSocket
        options.Limits.MaxRequestBodySize = limits.GetValue<long?>("MaxRequestBodySize"); // Kích thước tối đa của thân yêu cầu (10 MB)
        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(limits.GetValue<int>("KeepAliveTimeout")); // Thời gian chờ tối đa của kết nối Keep-Alive
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(limits.GetValue<int>("RequestHeadersTimeout")); // Thời gian chờ tối đa khi đọc header yêu cầu

        // Cấu hình Endpoints
        var endpoints = kestrelConfig.GetSection("Endpoints");
        var http = endpoints.GetSection("Http");
        if (http.Exists())
        {
            options.ListenAnyIP(5000); // Lắng nghe HTTP
        }

        var https = endpoints.GetSection("Https");
        if (https.Exists())
        {
            var certificatePath = https.GetValue<string>("Certificate:Path");
            var certificatePassword = https.GetValue<string>("Certificate:Password");
            if (!string.IsNullOrEmpty(certificatePath) && !string.IsNullOrEmpty(certificatePassword))
            {
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(certificatePath, certificatePassword);
                });
            }
        }

        // Tắt header "Server"
        var serverHeader = kestrelConfig.GetValue<bool>("AddServerHeader");
        if (serverHeader)
        {
            options.AddServerHeader = kestrelConfig.GetValue<bool>("AddServerHeader");
        }
    }

    // Tốc độ tối thiểu dữ liệu (byte/giây)
    var requestRate = limits.GetSection("MinRequestBodyDataRate");
    if (requestRate.Exists())
    {
        var bytesPerSecond = requestRate.GetValue<double>("BytesPerSecond");
        var gracePeriod = requestRate.GetValue<double>("GracePeriod");
        options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond, TimeSpan.FromSeconds(gracePeriod));
    }
    else
    {
        options.Limits.MinRequestBodyDataRate = null;
    }

    // Tốc độ tối thiểu phản hồi
    var responseRate = limits.GetSection("MinResponseDataRate");
    if (responseRate.Exists())
    {
        var bytesPerSecond = responseRate.GetValue<double>("BytesPerSecond");
        var gracePeriod = responseRate.GetValue<double>("GracePeriod");
        options.Limits.MinResponseDataRate = new MinDataRate(bytesPerSecond, TimeSpan.FromSeconds(gracePeriod));
    }
    else
    {
        options.Limits.MinResponseDataRate = null;
    }
});

var cORSWhileList = builder.Configuration.GetSection("IBoxConfig:CORSWhileList").Get<string[]>();
var cORSHeaderRequired = builder.Configuration.GetSection("IBoxConfig:CORSHeaderRequired").Get<string[]>();

int retainedFileCountLimit = builder.Configuration.GetValue<int>("IBoxConfig:retainedFileCountLimit", 60);
int fileSizeLimitBytes = builder.Configuration.GetValue<int>("IBoxConfig:fileSizeLimitBytes", 10_000_000);

string IBoxAllowSpecificOrigins = "CORS-Config";

if (cORSWhileList == null || cORSWhileList.Length == 0)
{
    cORSWhileList = new[] { "" };
}

if (cORSHeaderRequired == null || cORSHeaderRequired.Length == 0)
{
    cORSHeaderRequired = new[] { "" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: IBoxAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins(cORSWhileList);
                          builder.AllowCredentials();
                          builder.WithHeaders(cORSHeaderRequired);
                          builder.SetIsOriginAllowed(origin => true);
                          builder.SetIsOriginAllowedToAllowWildcardSubdomains();
                      });
});

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Map(
            keyPropertyName: "TenantId",
            defaultKey: "AppLogs",
            configure: (tenantId, log) => log.File(
                path: $"Logs/{tenantId}/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCountLimit,
                fileSizeLimitBytes: fileSizeLimitBytes,
                outputTemplate: "{Timestamp:o} [{Level:u3}] (Tenant={TenantId}) {Message:lj}{NewLine}{Exception}",
                encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            )
        );
});

var app = builder.Build();

app.UseCors(IBoxAllowSpecificOrigins);

var rootContext = app.Services.CreateScope().ServiceProvider.GetService<IBContext<RootContext>>();

if (rootContext != null && rootContext.Context.Database.CanConnect())
{
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    var scope = scopeFactory.CreateScope();
    var sqlDependency = scope.ServiceProvider.GetRequiredService<ISqlDependencyDatabase>() ?? throw new IboxLog("Can not Sql Dependency IBox", "AppLogs");
    sqlDependency.SqlDependencyStartService("RestService");

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopped.Register(() =>
    {
        Log.Information($"Dispose called at {DateTime.Now}, Service: {AppDomain.CurrentDomain.FriendlyName} Rest");
        sqlDependency.Dispose();
        scope.Dispose();
    });
}

var gConfig = app.Services.CreateScope().ServiceProvider.GetRequiredService<IIBGlobalConfig>();
gConfig.OnLoad("RestService");
gConfig.MailAlertRunService("RestService");

gConfig.LoadConfigRoot();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.Run();