using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class BaseTable
    {
        private string id = Guid.NewGuid().ToString();
        private DateTime? createdDate;
        private bool isDelete;
        private DateTime? modificationDate;

        /// <summary>
        /// Base properties
        /// </summary>
        [Key]
        [MaxLength(60)]
        [Description("Mã định danh bản ghi, tự động ghi ")]
        public string Id { get => id; set => id = value; }

        [Description("Ngày khởi tạo")]
        public DateTime? CreatedDate { get => createdDate; set => createdDate = value; }

        [Description("Trạng thái xóa")]
        [DefaultValue(false)]
        public bool IsDelete { get => isDelete; set => isDelete = value; }

        [Description("Ngày chỉnh sửa")]
        public DateTime? ModificationDate { get => modificationDate; set => modificationDate = value; }
    }
}