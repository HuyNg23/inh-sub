using IBox.Common;
using IBox.Common.Objects;
using IBox.Database.DBC;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx;
using IBox.MailService;
using IBox.PageBuilder;
using IBox.Permissions;
using IBox.Schedule.Library;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.ScheduleHistory;
using IBox.Workflow;
using Microsoft.AspNetCore.Mvc.Formatters;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Simpl;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var subFolders = new[] { "", "js", "lib" };

foreach (var folder in subFolders)
{
    var fullPath = Path.Combine(webRootPath, folder);
    if (!Directory.Exists(fullPath))
    {
        Directory.CreateDirectory(fullPath);
    }
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
    options.OutputFormatters.Add(new SystemTextJsonOutputFormatter(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<Configuration>(builder.Configuration.GetSection("IBoxConfig"));
builder.Services.AddServiceScheduleHistories();
builder.Services.AddServiceCommon();
builder.Services.AddServiceDLLEx();
builder.Services.AddServiceRootDB();
builder.Services.AddServiceTenantDB();
builder.Services.AddServiceDBC();
builder.Services.AddServiceMail();
builder.Services.AddServiceWorkflow();
builder.Services.AddServicePageBuilder();
builder.Services.AddServicePermissionsTenant();
builder.Services.AddServiceScheduleLibrary();
builder.Services.AddServiceGetDataLog();

//builder.WebHost.UseWebRoot("wwwroot");

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


builder.Services.AddQuartz(q =>
{
    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();
});

// Đăng ký IScheduler
builder.Services.AddSingleton(provider =>
{
    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    return schedulerFactory.GetScheduler().Result;
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: IBoxAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins(cORSWhileList ?? new string[] { "" });
                          builder.AllowCredentials();
                          builder.WithHeaders(cORSHeaderRequired ?? new string[] { "" });
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
    sqlDependency.SqlDependencyStartService("StoreService");

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopped.Register(() =>
    {
        sqlDependency.Dispose();
        scope.Dispose();
    });
}

var gConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gConfig.OnLoad("StoreService");
gConfig.LoadConfigRoot();

gConfig.MailAlertRunService("Store");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.UseStaticFiles();

app.Run();