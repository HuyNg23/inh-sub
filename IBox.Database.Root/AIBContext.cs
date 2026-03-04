using IBox.Common.Objects;
using IBox.Common.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IBox.Database.Root
{
    public abstract class AibContext : DbContext
    {
        protected IConfiguration configuration { get; set; }

        protected IEncryption encryption;

        protected AibContext(IConfiguration configuration, IEncryption encryption)
        {
            this.configuration = configuration;
            this.encryption = encryption;
        }

        protected string baseConenctionString = @"Data Source={0};Initial Catalog={1};User ID={2};Password={3};Connect Timeout={4};Encrypt=true;TrustServerCertificate=true;{5}";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var additionalOptions = string.Empty;

            if (this.configuration?.Config?.Value?.Database?.Options != null)
            {

                additionalOptions = string.Join(";", this.configuration?.Config?.Value?.Database?.Options?.Select(opt => $"{opt.Key}={opt.Value}"));

            }

            var serverName = this.configuration?.Config?.Value?.Database?.Main.ServerName;
            var databaseName = this.configuration?.Config?.Value?.Database?.Main.DatabaseName;
            var userName = this.configuration?.Config?.Value?.Database?.Main.UserName;
            var timeout = this.configuration?.Config?.Value?.Database?.Main.Timeout;
            
            var connectionString = string.Format(this.baseConenctionString,
                                                serverName,
                                                databaseName,
                                                userName,
                                                this.encryption.Decrypt(this.configuration?.Config?.Value?.Database?.Main?.Password ?? "SQLDefaultPassword"),
                                                timeout,
                                                additionalOptions);

            // Log connection string (with password masked)
            var maskedConnectionString = string.Format(this.baseConenctionString,
                                                serverName,
                                                databaseName,
                                                userName,
                                                "***MASKED***",
                                                timeout,
                                                additionalOptions);
            
            Log.Information($"[DB Connection] Attempting to connect to database with connection string: {maskedConnectionString}");
            Log.Information($"[DB Connection] ServerName: {serverName}, DatabaseName: {databaseName}, UserName: {userName}, Timeout: {timeout}");

            optionsBuilder.UseSqlServer(connectionString);
        }

        public void Update()
        {
            try
            {
                if (!this.Database.CanConnect())
                {
                    this.Database.Migrate();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}