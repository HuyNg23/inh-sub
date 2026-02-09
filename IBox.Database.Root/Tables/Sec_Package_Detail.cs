using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    [Index(nameof(Sec_Package.Name), IsUnique = true)]
    public class Sec_Package_Detail : BaseTable
    {
        private string? name;
        private string? path;
        private string? version;

        [Required]
        [Description("Tên gói mã hóa")]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Đường dẫn lưu trữ gói mã hóa")]
        [Required]
        public string? Path { get => path; set => path = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Phiên bản gói mã hóa")]
        [Required]
        public string? Version { get => version; set => version = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}