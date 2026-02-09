namespace IBox.Common.Objects
{
    public class MailAlertRootConfig
    {
        private string? smtp_Host;
        private string? smtp_Port;
        private string? smtp_Username;
        private string? smtp_Password;
        private bool? smtp_Credentials;
        private bool? smtp_EnableSSL;
        private string? mailTo;
        private string? mailCC;
        private string? mailBCC;
        private string? mailTitle;
        private string? smtp_Body;
        private bool? autoSendMail;
        private string? iD;
        private string? wfID;
        private string? stepID;

        public string? Smtp_Host { get => smtp_Host; set => smtp_Host = value; }
        public string? Smtp_Port { get => smtp_Port; set => smtp_Port = value; }
        public string? Smtp_Username { get => smtp_Username; set => smtp_Username = value; }
        public string? Smtp_Password { get => smtp_Password; set => smtp_Password = value; }
        public bool? Smtp_Credentials { get => smtp_Credentials; set => smtp_Credentials = value; }
        public bool? Smtp_EnableSSL { get => smtp_EnableSSL; set => smtp_EnableSSL = value; }
        public string? MailTo { get => mailTo; set => mailTo = value; }
        public string? MailCC { get => mailCC; set => mailCC = value; }
        public string? MailBCC { get => mailBCC; set => mailBCC = value; }
        public string? MailTitle { get => mailTitle; set => mailTitle = value; }
        public string? MailBody { get => smtp_Body; set => smtp_Body = value; }
        public bool? AutoSendMail { get => autoSendMail; set => autoSendMail = value; }
        public string? Id { get => iD; set => iD = value; }
        public string? WFID { get => wfID; set => wfID = value; }
        public string? StepID { get => stepID; set => stepID = value; }
        public TypeWarning typeWarning { get; set; }
    }

    public enum TypeWarning
    {
        APIUnlock = 0, //API được mở khóa
        WorkflowError = 1, //Workflow bị lỗi
        WorkflowWHError = 2, //WorkflowWH bị lỗi
        ChangeConfig = 3, //Thay đổi cấu hình Service Ibox
        LoginTenant = 4, //Đăng nhập tenant
        TenantDelete = 5, //Xóa tenant
        APINotFound = 6, //Không tìm thấy api
        ErrorRunSQL = 7, //Chạy câu querry bị lỗi
        CallAPIError = 8, //Call API bên thứ 3 lỗi
        WorkflowDeployment = 9, //Workflow được triển khai
        APIRecall = 10, //Workflow được thu hồi
        ServiceRunning = 11, //Service đang running
        CreateStepWF = 12, //Tạo step wf
        UpdateStepWF = 13, //Update step wf
        DeleteStepWF = 14, //Delete step WF
        ShrinkLogDb = 15,
        ErrorRunInformix = 16, // Chạy câu query Informix bị lỗi
        PageNotFound = 17,
        PageUnlock = 18,
        PageDeployment, // Page được triển khai
    }

    public enum TypeConfigRoot
    {
        MailAlertConfig = 0,
    }
}