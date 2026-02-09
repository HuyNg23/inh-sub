using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class P_Layout: BaseTable
    {
        [Required, MaxLength(450)]
        public string? Name { get; set; } = string.Empty;
    }
}
