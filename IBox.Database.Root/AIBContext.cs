using IBox.Common.Objects;
using IBox.Common.Security;
using Microsoft.EntityFrameworkCore;

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

            optionsBuilder.UseSqlServer(string.Format(this.baseConenctionString,
                                                this.configuration?.Config?.Value?.Database?.Main.ServerName,
                                                this.configuration?.Config?.Value?.Database?.Main.DatabaseName,
                                                this.configuration?.Config?.Value?.Database?.Main.UserName,
                                                this.encryption.Decrypt(this.configuration?.Config?.Value?.Database?.Main?.Password ?? "SQLDefaultPassword"),
                                                this.configuration?.Config?.Value?.Database?.Main.Timeout,
                                                additionalOptions
                                                ));
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