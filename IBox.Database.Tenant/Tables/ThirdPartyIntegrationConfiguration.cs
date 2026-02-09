using IBox.Database.Root.Tables;

namespace IBox.Database.Tenant.Tables
{
    public class ThirdPartyIntegrationConfiguration : BaseTable
    {
        private string? applicationName;
        private string? applicationKey;
        private string? config;

        public string? ApplicationName { get => applicationName; set => applicationName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? ApplicationKey { get => applicationKey; set => applicationKey = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? Config { get => config; set => config = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}