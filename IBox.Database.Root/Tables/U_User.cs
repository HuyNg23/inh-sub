using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class U_User : BaseTable
    {
        private string? userName;
        private string? password;
        private string? tenantID;
        private string? roleID;
        private string? sKey;
        private DateTime? sKey_Expires;

        [Required]
        [Description("Tài khoản đăng nhập cho user")]
        public string? UserName { get => userName; set => userName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        [Description("mật khẩu đăng nhập cho user, đã mã hóa")]
        public string? Password { get => password; set => password = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Mã định danh khách hàng, tham chiếu bảng tenant")]
        [MaxLength(60)]
        public string? TenantID { get => tenantID; set => tenantID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Mã định danh quyền sử dụng")]
        [MaxLength(60)]
        public string? RoleID { get => roleID; set => roleID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Mã an toàn cho mỗi phiên truy cập")]
        public string? SKey { get => sKey; set => sKey = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Thời gian tồn tại của mã an toàn")]
        public DateTime? SKey_Expires { get => sKey_Expires; set => sKey_Expires = value; }
    }
}