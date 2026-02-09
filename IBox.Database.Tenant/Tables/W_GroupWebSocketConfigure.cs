using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class W_GroupWebSocketConfigure : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        private string? name;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}