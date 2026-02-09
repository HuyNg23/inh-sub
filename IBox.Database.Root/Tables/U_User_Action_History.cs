using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    [Index(nameof(U_User_Action_History.UserID))]
    public class U_User_Action_History : BaseTable
    {
        private string? userID;
        private UserAction? userAction;
        private ActionStatus? actionStatus;
        private string? actionName;

        [MaxLength(60)]
        [Required]
        [Description("Mã định danh người dùng, tham chiếu bảng User")]
        public string? UserID { get => userID; set => userID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Hành động của người dùng, tham chiếu định nghĩa enum User Action")]
        [DefaultValue(0)]
        public UserAction? UserAction { get => userAction; set => userAction = value; }

        [Description("Trạng thái hành động của khách hàng, tham chiếu enum Action Status")]
        [DefaultValue(0)]
        public ActionStatus? ActionStatus { get => actionStatus; set => actionStatus = value; }

        [Description("Tên hành động")]
        public string? ActionName { get => actionName; set => actionName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }

    public enum UserAction
    {
        Login,
        Logout,
        Create,
        Update,
        RollBack,
        Delete,
        DownLoad
    }

    public enum ActionStatus
    {
        Fail,
        Success
    }

    public enum ActionName
    {
        ConfigTenant
    }
}