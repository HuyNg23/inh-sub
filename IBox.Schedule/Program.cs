using IBox.Common;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Database.DBC;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx;
using IBox.MailService;
using IBox.Permissions;
using IBox.Schedule.Library;
using IBox.Schedule.Library.Execution;
using IBox.Schedule.Library.HandleJobSchedule;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Schedule.Library.HubSchedule;
using IBox.ScheduleHistory;
using IBox.Workflow;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Simpl;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<Configuration>(builder.Configuration.GetSection("IBoxConfig"));
//builder.Services.AddScoped<IWebSocketGlobalConfig, WebSocketGlobalConfig>();
//builder.Services.AddScoped<IConnectWebSocket, ConnectWebSocket>();
builder.Services.AddServiceCommon();
builder.Services.AddServiceTenantDB();
builder.Services.AddServiceRootDB();
builder.Services.AddServiceDBC();
builder.Services.AddServiceWorkflow();
builder.Services.AddServiceDLLEx();
builder.Services.AddServiceMail();
builder.Services.AddServiceScheduleHistories();
builder.Services.AddSignalR();
builder.Services.AddServiceScheduleLibrary();
builder.Services.AddServicePermissionsTenant();
builder.Services.AddServiceGetDataLog();
string IBoxAllowSpecificOrigins = "CORS-Config";
var cORSWhileList = builder.Configuration.GetSection("IBoxConfig:CORSWhileList").Get<string[]>();
var cORSHeaderRequired = builder.Configuration.GetSection("IBoxConfig:CORSHeaderRequired").Get<string[]>();

var timeCronJobInsertSchedule = builder.Configuration.GetSection("Schedule:RuntimeSchedule").Get<string>();
var timeCronJobExecutePlan = builder.Configuration.GetSection("Schedule:RuntimeJobAutoStart").Get<string>();

int retainedFileCountLimit = builder.Configuration.GetValue<int>("IBoxConfig:retainedFileCountLimit", 60);
int fileSizeLimitBytes = builder.Configuration.GetValue<int>("IBoxConfig:fileSizeLimitBytes", 10_000_000);

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

builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
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
                                         .UsingJobData("typeUserBase", ((int?)TypeUserBase.Tenant).ToString()));
    q.AddTrigger(opts => opts.ForJob(jobPingHub).WithCronSchedule("0/20 * * ? * * *"));

    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();
    var jobClearEndpoints = new JobKey("Clear_End_points", "Job_ClearEndpoints");
    q.AddJob<ExecuteClearEndpoints>(opts => opts.WithIdentity(jobClearEndpoints));
    q.AddTrigger(opts => opts.ForJob(jobClearEndpoints).WithCronSchedule("0 0 0/2 ? * * *"));
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

var rootContext = app.Services.CreateScope().ServiceProvider.GetService<IBContext<RootContext>>();

if (rootContext != null && rootContext.Context.Database.CanConnect())
{
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    var scope = scopeFactory.CreateScope();
    var sqlDependency = scope.ServiceProvider.GetRequiredService<ISqlDependencyDatabase>() ?? throw new IboxLog("Can not Sql Dependency IBox", "AppLogs");

    sqlDependency.OnLoadSqlDependency(TypeUserBase.Tenant);
    sqlDependency.SqlDependencyStartService("ScheduleService");

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopped.Register(() =>
    {
        Log.Information($"Dispose called at {DateTime.Now}, Service: {AppDomain.CurrentDomain.FriendlyName} Schedule");
        sqlDependency.Dispose();
        scope.Dispose();
    });
}

var gConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gConfig.OnLoad("ScheduleService");
gConfig.LoadConfigRoot();
gConfig.MailAlertRunService("Schedule");

var RunSchedule = app.Services.CreateScope().ServiceProvider.GetService<IHandleJobsSchedule>() ?? throw new IboxLog("Can not load global config", "AppLogs");
RunSchedule.RunScheduleTenant();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHubSchedule>("/ChatHubSchedule");
});

app.MapControllers();

app.Run();