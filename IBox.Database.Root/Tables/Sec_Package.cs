using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    [Index(nameof(Sec_Package.Name), IsUnique = true)]
    public class Sec_Package : BaseTable
    {
        private string? name;
        private string? description;

        [Description("Tên gói mã hóa"), Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Mô tả gói mã hóa")]
        public string? Description { get => description; set => description = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}