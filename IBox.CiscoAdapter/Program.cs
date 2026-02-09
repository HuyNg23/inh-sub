using IBox.CiscoAdapter.Services;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddEndpointsApiExplorer();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add HttpClient Factory
builder.Services.AddHttpClient();

// Register Token Cache Service
builder.Services.AddSingleton<ITokenCacheService, TokenCacheService>();

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
var corsHeaders = builder.Configuration.GetSection("CorsSettings:AllowedHeaders").Get<string[]>() ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CiscoAdapterCorsPolicy", builder =>
    {
        if (corsOrigins.Contains("*"))
        {
            builder.AllowAnyOrigin();
        }
        else
        {
            builder.WithOrigins(corsOrigins);
        }

        if (corsHeaders.Contains("*"))
        {
            builder.AllowAnyHeader();
        }
        else
        {
            builder.WithHeaders(corsHeaders);
        }

        builder.AllowAnyMethod();
    });
});

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            path: "Logs/log.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 10_000_000,
            outputTemplate: "{Timestamp:o} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("CiscoAdapterCorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

Log.Information("IBox.CiscoAdapter service started");

app.Run();
