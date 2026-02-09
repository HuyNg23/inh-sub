using IBox.Common.TCP;
using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(WF_Define.Name), IsUnique = true)]
    public class WF_Define : BaseTable
    {
        private string? name;
        private string? description = string.Empty;
        private bool? isLock = false;
        private bool? isDeploy = false;
        private string? userName = string.Empty;
        private string? password = string.Empty;
        private AuthorType? authenType = AuthorType.None;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Description { get => description; set => description = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [DefaultValue(false)]
        public bool? IsLock { get => isLock; set => isLock = value; }

        [DefaultValue(false)]
        public bool? IsDeploy { get => isDeploy; set => isDeploy = value; }

        [MaxLength(450)]
        public string? UserName { get => userName; set => userName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(450)]
        public string? Password { get => password; set => password = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [DefaultValue(0)]
        public AuthorType? AuthenType { get => authenType; set => authenType = value; }

        [Description("Id người tạo bản ghi")]
        [MaxLength(60)]
        public string CreatedById { get; set; } = string.Empty;
    }
}