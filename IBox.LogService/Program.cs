using IBox.ChatBot.DB;
using IBox.ChatBot.HandleScheduleSQL;
using IBox.ChatBot.History;
using IBox.ChatBot.Schedule.DB.Schedule;
using IBox.Common;
using IBox.Common.Chat.Configuration;
using IBox.Common.ConsistentHashing;
using IBox.Common.Objects;
using IBox.Database.ChatBot;
using IBox.Database.DBC;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.ServicesManager;
using IBox.DLEx;
using IBox.History.DB;
using IBox.History.DB.Schedule.HistoryIBox;
using IBox.MailService;
using IBox.Permissions;
using IBox.Schedule.Library;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Workflow;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddServiceRootDB();
builder.Services.AddServiceTenantDB();
builder.Services.AddServiceSqliteDB();
builder.Services.AddServiceWorkflow();
builder.Services.AddServiceDLLEx();
builder.Services.AddServiceDBC();
builder.Services.AddServiceMail();
builder.Services.AddServiceChatBotHistory();
builder.Services.AddServiceChatBotDB();
builder.Services.AddServicePermissionsTenant();
builder.Services.AddServiceHistoryDB();
builder.Services.AddServiceScheduleLibrary();
builder.Services.AddServiceGetDataLog();
builder.Services.AddControllers()
           .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

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

string path = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseChatBot);
string pathHistory = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseLogIBox);

if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

builder.Services.AddSingleton(provider =>
{
    var optionsBuilder = new DbContextOptionsBuilder<DBChatDayContext>();
    optionsBuilder.UseSqlite("Data Source=:memory:");
    return optionsBuilder.Options;
});

builder.Services.AddSingleton(provider =>
{
    var optionsBuilder = new DbContextOptionsBuilder<DBHistoryContext>();
    optionsBuilder.UseSqlite("Data Source=:memory:");
    return optionsBuilder.Options;
});

builder.Services.Configure<QuartzOptions>(opts =>
{
    opts.Scheduling.IgnoreDuplicates = true;
    opts.Scheduling.OverWriteExistingData = true;
});
builder.Services.AddQuartz(q =>
{
    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();

    var jobExecuteScheduleCreateDB = new JobKey("Create_DB_Chat_Master", "Job_Create_DB_Chat_Master");
    q.AddJob<ExecuteScheduleCreateDB>(opts => opts.WithIdentity(jobExecuteScheduleCreateDB));
    q.AddTrigger(opts => opts.ForJob(jobExecuteScheduleCreateDB).WithCronSchedule("0 1 0 ? * * *"));

    var jobExecuteScheduleEndChat = new JobKey("End_Chat", "Job_End_Chat");
    q.AddJob<ExecuteScheduleEndChat>(opts => opts.WithIdentity(jobExecuteScheduleEndChat));
    q.AddTrigger(opts => opts.ForJob(jobExecuteScheduleEndChat).WithCronSchedule("0 0/10 * ? * * *"));

    var jobExecuteReadFileCustomer = new JobKey("Read_File", "Job_Read_FileCustomer");
    q.AddJob<ExecuteScheduleReadFileCustomer>(opts => opts.WithIdentity(jobExecuteReadFileCustomer));
    q.AddTrigger(opts => opts.ForJob(jobExecuteReadFileCustomer).WithCronSchedule("0/10 * * ? * * *"));

    var jobExecuteScheduleReadFile = new JobKey("Read_File", "Job_Read_File");
    q.AddJob<ExecuteScheduleReadFile>(opts => opts.WithIdentity(jobExecuteScheduleReadFile));
    q.AddTrigger(opts => opts.ForJob(jobExecuteScheduleReadFile).WithCronSchedule("0/5 * * ? * * *"));

    var jobExecuteScheduleZipFile = new JobKey("Zip_File", "Job_Zip_File");
    q.AddJob<ExecuteScheduleZipFile>(opts => opts.WithIdentity(jobExecuteScheduleZipFile));
    q.AddTrigger(opts => opts.ForJob(jobExecuteScheduleZipFile).WithCronSchedule("0 3 0 ? * * *"));

    var jobExecuteRemoveFileZip = new JobKey("Remove_File", "Job_Remove_FileZip");
    q.AddJob<ExecuteScheduleRemoveZip>(opts => opts.WithIdentity(jobExecuteRemoveFileZip));
    q.AddTrigger(opts => opts.ForJob(jobExecuteRemoveFileZip).WithCronSchedule("0 1 0 ? * * *"));

    #region Job đọc lịch sử api đẩy vào db

    var jobExecuteReadFileHistoryAPI = new JobKey("Read_File", "Job_Read_FileHistory_API");
    q.AddJob<ExecuteScheduleReadFileHistoryAPI>(opts => opts.WithIdentity(jobExecuteReadFileHistoryAPI));
    q.AddTrigger(opts => opts.ForJob(jobExecuteReadFileHistoryAPI).WithCronSchedule("0/5 * * ? * * *"));

    var jobExecuteReadFileHistoryWF = new JobKey("Read_File", "Job_Read_FileHistory_WF");
    q.AddJob<ExecuteScheduleReadFileHistoryWF>(opts => opts.WithIdentity(jobExecuteReadFileHistoryWF));
    q.AddTrigger(opts => opts.ForJob(jobExecuteReadFileHistoryWF).WithCronSchedule("0/5 * * ? * * *"));

    var jobExecuteReadFileHistorySQL = new JobKey("Read_File", "Job_Read_FileHistory_SQL");
    q.AddJob<ExecuteScheduleReadFileHistorySQL>(opts => opts.WithIdentity(jobExecuteReadFileHistorySQL));
    q.AddTrigger(opts => opts.ForJob(jobExecuteReadFileHistorySQL).WithCronSchedule("0/5 * * ? * * *"));

    var Job_Update_Historical_Results_DB = new JobKey("Read_File", "Job_Update_Historical_Results_DB");
    q.AddJob<ExecuteScheduleUpdateHistoricalResultsDB>(opts => opts.WithIdentity(Job_Update_Historical_Results_DB));
    q.AddTrigger(opts => opts.ForJob(Job_Update_Historical_Results_DB).WithCronSchedule("0 0 0/7 ? * * *"));
    //q.AddTrigger(opts => opts.ForJob(Job_Update_Historical_Results_DB).WithCronSchedule("* * * ? * * *"));

    var Job_Remove_File_UnZip = new JobKey("Read_File", "Job_Remove_File_UnZip");
    q.AddJob<ExecuteRemoveFileUnZip>(opts => opts.WithIdentity(Job_Remove_File_UnZip));
    q.AddTrigger(opts => opts.ForJob(Job_Remove_File_UnZip).WithCronSchedule("0 30 * ? * * *"));

    #endregion Job đọc lịch sử api đẩy vào db
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

    sqlDependency.SqlDependencyStartService("LogService");

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopped.Register(() =>
    {
        sqlDependency.Dispose();
        scope.Dispose();
    });
}

var gConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gConfig.OnLoad("LogService");
gConfig.MailAlertRunService("LogService");

var gTenantConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalTenantConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gTenantConfig.OnLoadConfigChatBot();

gConfig.LoadConfigRoot();

try
{
    List<string> chatbots = new List<string>();
    for (int j = 1; j <= 100; j++)
    {
        chatbots.Add($"chatbot_{j}.db");
    }

    ConsistentHashing.AddListChatBotDBVirtual(chatbots);
}
catch (Exception ex)
{
    Log.Error($"AddListChatBotDBVirtual: {ex.Message}");
}

var getDataLogStream = app.Services.CreateScope().ServiceProvider.GetService<IGetDataLogStream>();
if (getDataLogStream != null)
{
    getDataLogStream.GetDataLog();
}

if (IBGlobalConfig.Tenants.Any())
{
    try
    {
        foreach (var tenant in IBGlobalConfig.Tenants)
        {
            using (var scope = app.Services.CreateScope())
            {
                var createDBChat = scope.ServiceProvider.GetService<ICreateDB>();
                if (createDBChat != null)
                {
                    createDBChat.CreateDBSQLiteHistory(tenant.Id);
                    createDBChat.CreateDBLiteChatBot(tenant.Id);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error($"CreateDBLiteHistory: {ex.Message}");
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.Run();