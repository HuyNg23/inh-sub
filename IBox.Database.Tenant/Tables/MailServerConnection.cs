using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(MailServerConnection.Smtp_Host), nameof(MailServerConnection.Smtp_Port), IsUnique = true)]
    public class MailServerConnection : BaseTable
    {
        private string? smtp_Host;
        private string? smtp_Port;
        private string? smtp_Username;
        private string? smtp_Password;
        private bool? smtp_Credentials;
        private bool? smtp_EnableSSL;

        [Required]
        public string? Smtp_Host { get => smtp_Host; set => smtp_Host = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? Smtp_Port { get => smtp_Port; set => smtp_Port = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Smtp_Username { get => smtp_Username; set => smtp_Username = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? Smtp_Password { get => smtp_Password; set => smtp_Password = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public bool? Smtp_Credentials { get => smtp_Credentials; set => smtp_Credentials = value; }
        public bool? Smtp_EnableSSL { get => smtp_EnableSSL; set => smtp_EnableSSL = value; }
    }
}