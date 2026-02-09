using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class DatabaseConnection : BaseTable
    {
        private string? address;
        private string? port;
        private string? username;
        private string? password;
        private string? catalog;
        private int sQLType;

        [Required]
        public string? Address { get => address; set => address = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Port { get => port; set => port = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? Username { get => username; set => username = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? Password { get => password; set => password = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? Catalog { get => catalog; set => catalog = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public int SQLType { get => sQLType; set => sQLType = value; }
        public string Options { get; set; } = string.Empty;
    }
}