using IBox.ChatBot.DB;
using IBox.ChatBot.Handel;
using IBox.ChatBot.History;
using IBox.Common;
using IBox.Common.Objects;
using IBox.Database.ChatBot;
using IBox.Database.DBC;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx;
using IBox.Permissions;
using IBox.Schedule.Library;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Workflow;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<Configuration>(builder.Configuration.GetSection("IBoxConfig"));
builder.Services.AddScoped<IBox.Common.Objects.IConfiguration, Configuration>();
builder.Services.AddServiceCommon();
builder.Services.AddServiceRootDB();
builder.Services.AddServiceTenantDB();
builder.Services.AddServiceSqliteDB();
builder.Services.AddServiceWorkflow();
builder.Services.AddServiceChatBotHandel();
builder.Services.AddServiceDLLEx();
builder.Services.AddServiceDBC();
builder.Services.AddServiceChatBotHistory();
builder.Services.AddServiceChatBotDB();
builder.Services.AddServicePermissionsTenant();
builder.Services.AddServiceScheduleLibrary();
builder.Services.AddServiceGetDataLog();
builder.Services.AddControllers()
           .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

var cORSWhileList = builder.Configuration.GetSection("IBoxConfig:CORSWhileList").Get<string[]>();
var cORSHeaderRequired = builder.Configuration.GetSection("IBoxConfig:CORSHeaderRequired").Get<string[]>();

int retainedFileCountLimit = builder.Configuration.GetValue<int>("IBoxConfig:retainedFileCountLimit", 60);
int fileSizeLimitBytes = builder.Configuration.GetValue<int>("IBoxConfig:fileSizeLimitBytes", 10_000_000);

string IBoxAllowSpecificOrigins = "CORS-Config";

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


var rootContext = app.Services.CreateScope().ServiceProvider.GetService<IBContext<RootContext>>();

if (rootContext != null && rootContext.Context.Database.CanConnect())
{
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    var scope = scopeFactory.CreateScope();
    var sqlDependency = scope.ServiceProvider.GetRequiredService<ISqlDependencyDatabase>() ?? throw new IboxLog("Can not Sql Dependency IBox", "AppLogs");

    sqlDependency.SqlDependencyStartService("ChatBotService");
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopped.Register(() =>
    {
        sqlDependency.Dispose();
        scope.Dispose();
    });
}

var gConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gConfig.OnLoad("ChatBotService");

var gTenantConfig = app.Services.CreateScope().ServiceProvider.GetService<IIBGlobalTenantConfig>() ?? throw new IboxLog("Can not load global config", "AppLogs");
gTenantConfig.OnLoadConfigChatBot();
gConfig.LoadConfigRoot();

app.UseCors(IBoxAllowSpecificOrigins);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.Run();