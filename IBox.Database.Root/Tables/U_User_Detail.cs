using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace IBox.Database.Root.Tables
{
    [Index(nameof(U_User_Detail.UserID))]
    public class U_User_Detail : BaseTable
    {
        private string? userID;
        private string? fullName;
        private string? avatar;

        [Description("Mã định danh người dùng, tham chiếu bảng User")]
        public string? UserID { get => userID; set => userID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Tên đầy đủ của tài khoản")]
        public string? FullName { get => fullName; set => fullName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Đường dẫn ảnh đại diện dành cho tài khoản")]
        public string? Avatar { get => avatar; set => avatar = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}