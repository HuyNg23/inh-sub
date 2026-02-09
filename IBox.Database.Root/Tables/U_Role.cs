using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class U_Role : BaseTable
    {
        [Description("Tên quyền")]
        [MaxLength(450)]
        [Required]
        public string Name { get; set; } = string.Empty;

        [MaxLength(60)]
        [Description("Mã định danh của tenant")]
        [Required]
        public string TenantId { get; set; } = string.Empty;

        [MaxLength(4000)]
        [Description("Danh sách action của quyền")]
        public string PermissionsKey { get; set; } = "[]";
    }
}