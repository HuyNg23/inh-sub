using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteDBChatDay.TableDBHistory
{
    public class BaseTableSQLite
    {
        private string id = Guid.NewGuid().ToString();

        /// <summary>
        /// Base properties
        /// </summary>
        [Key]
        [MaxLength(60)]
        [Description("Mã định danh bản ghi, tự động ghi ")]
        public string Id { get => id; set => id = value; }

        [Description("Ngày khởi tạo")]
        public long CreatedDate { get; set; } = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

        [Description("Trạng thái xóa")]
        [DefaultValue(false)]
        public bool IsDelete { get; set; } = false;

        [Description("Ngày chỉnh sửa")]
        public long ModificationDate { get; set; }

        /// <summary>
        /// trigger được kích hoạt khi có sự kiểm thêm mới dữ liệu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void Addedd_Trigger(object? sender, Microsoft.EntityFrameworkCore.ChangeTracking.DetectedEntityChangesEventArgs e)
        { }

        /// <summary>
        /// Trigger được kích hoạt khi có sự kiện chỉnh sửa dữ liệu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void Modified_Trigger(object? sender, Microsoft.EntityFrameworkCore.ChangeTracking.DetectedEntityChangesEventArgs e)
        { }
    }
}