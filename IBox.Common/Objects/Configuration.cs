using Microsoft.Extensions.Options;

namespace IBox.Common.Objects
{
    public class Configuration : IConfiguration
    {
        private Database database = new Database();

        public Database Database { get => database; set => database = value; }
        public int ZipFileDay { get; set; }
        public int MaxZipFileCount { get; set; } = 10;
        public string Port { get; set; } = "0";
        public string SubDomain { get; set; } = "";
        public int TypeProtocol { get; set; } = 0;
        public JWT JWT { get; set; }
        public IOptions<Configuration> Config { get; internal set; }
        public Configuration()
        { }

        public Configuration(IOptions<Configuration> configuration)
        {
            this.Config = configuration;
        }
    }

    public class Database
    {
        private MainDatabase main = new MainDatabase();
        private List<OptionSQL> option = new List<OptionSQL>();

        public MainDatabase Main { get => main; set => main = value; }
        public List<OptionSQL> Options { get => option; set => option = value; }
    }

    public class MainDatabase
    {
        private string serverName = string.Empty;
        private string databaseName = string.Empty;
        private string password = string.Empty;
        private string userName = string.Empty;
        private int timeout = 10;

        public string ServerName { get => serverName; set => serverName = value; }
        public string DatabaseName { get => databaseName; set => databaseName = value; }
        public string Password { get => password; set => password = value; }
        public string UserName { get => userName; set => userName = value; }
        public int Timeout { get => timeout; set => timeout = value; }
    }

    public class JWT
    {
        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public int TotalMinuteAlive { get; set; }
    }

    public class PasswordConvention
    {
        public string? Convention { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class OptionSQL
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}