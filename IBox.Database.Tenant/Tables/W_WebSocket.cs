using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class W_WebSocket : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        private string? name;
        private string? domain;
        private int port = 0;
        private string? site;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? Domain { get => domain; set => domain = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public int Port { get => port; set => port = value; }

        [Required]
        public string? Site { get => site; set => site = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}