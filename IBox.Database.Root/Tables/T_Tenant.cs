using IBox.Common.Objects;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class T_Tenant : BaseTable
    {
        private string? tenantName;
        private string? tenantCode;
        private string? email;
        private string? phone;
        private string? address;
        private string? smtp_Host;
        private string? smtp_Port;
        private string? smtp_Username;
        private string? smtp_Password;
        private string? mailTo;
        private string? mailCC;
        private string? mailBCC;
        private string? mailTitle;
        private string? mailBody;

        [Required]
        [Description("Tên khách hàng triển khai IBox")]
        public string? TenantName { get => tenantName; set => tenantName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Mã khách hàng được triển khai, sinh tự động bởi hệ thống")]
        public string? TenantCode { get => tenantCode; set => tenantCode = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Địa chỉ email khách hàng, không bắt buộc")]
        public string? Email { get => email; set => email = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Số điện thoại khách hàng, không bắt buộc")]
        public string? Phone { get => phone; set => phone = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Địa chỉ khách hàng, không bắt buộc")]
        public string? Address { get => address; set => address = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Host mail SMTP")]
        public string? Smtp_Host { get => smtp_Host; set => smtp_Host = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Port mail SMTP")]
        public string? Smtp_Port { get => smtp_Port; set => smtp_Port = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Tài khoản mail")]
        public string? Smtp_Username { get => smtp_Username; set => smtp_Username = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Mật khẩu mail SMTP")]
        public string? Smtp_Password { get => smtp_Password; set => smtp_Password = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Xác thực SMTP (SMTP Authentication hoặc SMTP AUTH)")]
        [DefaultValue(false)]
        public bool Smtp_Credentials { get; set; }

        [Description("Check ssl")]
        [DefaultValue(false)]
        public bool Smtp_EnableSSL { get; set; }

        [Description("Gửi đến mail ...")]
        public string? MailTo { get => mailTo; set => mailTo = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Gửi mail CC")]
        public string? MailCC { get => mailCC; set => mailCC = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Gửi cho nhiều người cùng lúc và không biết danh sách những người nào được gửi")]
        public string? MailBCC { get => mailBCC; set => mailBCC = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Title mail")]
        public string? MailTitle { get => mailTitle; set => mailTitle = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Nội dung mail")]
        public string? MailBody { get => mailBody; set => mailBody = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Check send mail tự động")]
        [DefaultValue(false)]
        public bool AutoSendMail { get; set; }

        //[MaxLength(450)]
        //[Description("Định danh domain cho từng tennat")]
        //public string DomainEmail { get; set; } = string.Empty;

        /// <summary>
        /// Thực hiện sinh mã khách hàng dựa trên tên khách hàng
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public string FormatToCode(string source, string tenantId)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new IboxLog("Tenant Name is required", tenantId);
            }
            string result = string.Empty;
            foreach (string item in source.Trim().Split(' '))
            {
                if (string.IsNullOrEmpty(item[0].ToString()))
                {
                    continue;
                }
                result += item.FirstOrDefault();
            }
            DateTime date = DateTime.Now;
            return string.Format("{0}_{1}", result, date.ToFileTime());
        }
    }
}