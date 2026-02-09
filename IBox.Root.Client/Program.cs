using IBox.Common;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Database.DBC;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.UpdateVersion;
using IBox.Database.UpdateVersion.Factories;
using IBox.DLEx;
using IBox.MailService;
using IBox.PageBuilder;
using IBox.Schedule.Library;
using IBox.Schedule.Library.Execution;
using IBox.Schedule.Library.HandleJobSchedule;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Schedule.Library.HubSchedule;
using IBox.Workflow;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Simpl;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<Configuration>(builder.Configuration.GetSection("IBoxConfig"));
builder.Services.AddServiceCommon();
builder.Services.AddServiceTenantDB();
builder.Services.AddServiceWorkflow();
builder.Services.AddServiceDBC();
builder.Services.AddServiceDLLEx();
builder.Services.AddServiceMail();
builder.Services.AddServiceRootDB();
builder.Services.AddSignalR();
builder.Services.AddServiceScheduleLibrary();
builder.Services.AddServicePageBuilder();
builder.Services.AddServiceUpdateVersionDB();
builder.Services.AddServiceGetDataLog();

var cORSWhileList = builder.Configuration.GetSection("IBoxConfig:CORSWhileList").Get<string[]>();
var cORSHeaderRequired = builder.Configuration.GetSection("IBoxConfig:CORSHeaderRequired").Get<string[]>();

var timeCronJobInsertSchedule = builder.Configuration.GetSection("Schedule:RuntimeSchedule").Get<string>();
var timeCronJobExecutePlan = builder.Configuration.GetSection("Schedule:RuntimeJobAutoStart").Get<string>();

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

builder.Services.Configure<QuartzOptions>(opts =>
{
    opts.Scheduling.IgnoreDuplicates = true; // default: false
    opts.Scheduling.OverWriteExistingData = true; // default: true
});

builder.Services.AddQuartz(q =>
{
    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();

    var jobPingHub = new JobKey("Ping_Hub", "Job_Run_Daily");
    q.AddJob<ExecutePingHub>(opts => opts.WithIdentity(jobPingHub)
                                         .UsingJobData("typeUserBase", ((int?)TypeUserBase.Root).ToString()));
    q.AddTrigger(opts => opts.ForJob(jobPingHub).WithCronSchedule("0/20 * * ? * * *"));
});

builder.Services.AddSingleton<IScheduler>(provider =>
{
    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    var scheduler = schedulerFactory.GetScheduler().Result;
    scheduler.Start();
    return scheduler;
});

builder.Services.AddQuartzServer(options =>
{
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();

app.UseCors(IBoxAllowSpecificOrigins);

app.UseHttpsRedirection();

var rootContext = app.Services.CreateScope().ServiceProvider.GetService<IBContext<RootContext>>();

if (rootContext != null && rootContext.Context.Database.CanConnect())
{
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    var scope = scopeFactory.CreateScope();
    var sqlDependency = scope.ServiceProvider.GetRequiredService<ISqlDependencyDatabase>() ?? throw new IboxLog("Can not Sql Dependency IBox", "AppLogs");
    sqlDependency.OnLoadSqlDependency(TypeUserBase.Root);
    sqlDependency.SqlDependencyStartService("RootBEService");

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopped.Register(() =>
    {
        sqlDependency.Dispose();
        scope.Dispose();
    });

    //using var scope = app.Services.CreateScope();
    //var tenantService = scope.ServiceProvider.GetService<ITenantManagementService>() ?? throw new IboxException("Can not get tenantService");
    //tenantService.SynchronizeAllTenants();
}

var gConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gConfig.OnLoad("RootBEService");
gConfig.LoadConfigRoot();
gConfig.MailAlertRunService("RootBE");

var RunSchedule = app.Services.CreateScope().ServiceProvider.GetService<IHandleJobsSchedule>() ?? throw new IboxLog("Can not load global config", "AppLogs");
RunSchedule.RunScheduleRoot();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHubSchedule>("/ChatHubSchedule");
});

app.MapControllers();

app.Run();