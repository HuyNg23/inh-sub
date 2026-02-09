using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class D_DatabaseConnection : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        [Required]
        [Description("Địa chỉ server SQL")]
        public string? Address { get; set; }

        [Required]
        [Description("User đăng nhập")]
        public string? Username { get; set; }

        [Required]
        [Description("Mật khẩu")]
        public string? Password { get; set; }

        [Description("Thời gian chờ kết nối SQL")]
        public int? Timeout { get; set; }

        [Description("Đường dẫn lưu file khi shrink log")]
        public string? PathSaveLog { get; set; }

        [Required]
        [Description("Loại triển khai SQL là StandAlone hoặc AlwaysOn")]
        public DeploymentType? DeploymentType { get; set; }
    }

    public enum DeploymentType
    {
        StandAlone = 0,
        AlwaysOn = 1
    }
}