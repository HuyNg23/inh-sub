using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(GroupDynamicLinkedLibrary.Name), IsUnique = true)]
    public class GroupDynamicLinkedLibrary : BaseTable
    {
        private string? name;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}