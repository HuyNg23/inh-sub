using IBox.Database.Root.Tables;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class CB_ConfigChat : BaseTable
    {
        private string tokenBOTSendM = "";
        private string token_IC = "";
        private string listToken = "";
        private string url_Api_IC_UploadFile = "";
        private string url_Api_IC_SendMessage = "";
        private string url_Api_IC_DisableBot = "";
        private string url_Api_IC_EnableBot = "";
        private string idBOT = "";
        private string tenant_id1 = "";

        [Required]
        public string IdBOT { get => idBOT; set => idBOT = value; }

        public string EndChatTime { get; set; } = "24";
        public string TokenBOTSendChatBot { get => tokenBOTSendM; set => tokenBOTSendM = value; }
        public string IC_Token { get => token_IC; set => token_IC = value; }
        public string UserChatBot { get; set; } = "admin";
        public string PassChatBot { get; set; } = "";
        public string tenant_id { get => tenant_id1; set => tenant_id1 = value; }
        public TypeChat TypeChat { get; set; } = TypeChat.ChatBot;
        public string UrlShowChatIBox { get; set; } = "";
        public string Retry { get; set; } = "";

        #region Event

        public string? Ev_User_Support { get; set; } = "";
        public string? Ev_Predict { get; set; } = "";
        public string? Ev_EndChat { get; set; } = "";
        public string? Ev_Tag { get; set; } = "";

        #endregion Event

        #region BOT FPT

        public string? ListToken { get => listToken; set => listToken = value; }
        public string? Url_IC { get; set; } = "";
        public string? Url_Api_BOT_UploadFile { get => url_Api_IC_UploadFile; set => url_Api_IC_UploadFile = value; }
        public string? Url_Api_BOT_SendMessage { get => url_Api_IC_SendMessage; set => url_Api_IC_SendMessage = value; }
        public string? Url_Api_BOT_DisableBot { get => url_Api_IC_DisableBot; set => url_Api_IC_DisableBot = value; }
        public string Url_Api_BOT_EnableBot { get => url_Api_IC_EnableBot; set => url_Api_IC_EnableBot = value; }

        #endregion BOT FPT

        #region WF IBox

        [Required]
        [Description("Bắt buộc cấu hình WF cho event gửi tin nhắn từ BOT đến IC")]
        public string WF_SendChatIC { get; set; } = "";

        [Required]
        [Description("Bắt buộc cấu hình WF cho luồng định danh gửi vào CRM")]
        public string WF_CustomerIdentification { get; set; } = "";

        [Required]
        [Description("Bắt buộc cấu hình WF cho interaction đẩy thông tin vào crm khi tin nhắn đầu tiên")]
        public string WF_InteractionCRM { get; set; } = "";

        [Required]
        [Description("Bắt buộc cấu hình WF cho interaction update lại interaction CRM theo dạng batch")]
        public string WF_UpdateInteractionCRM { get; set; } = "";

        [Required]
        [Description("Bắt buộc cấu hình WF đẩy toàn bộ tin nhắn vào IC khi Agent cần can thiệp")]
        public string WF_PutAllChatIC { get; set; } = "";

        [Description("Cập nhật IC truyền ContactId, InteractionId và cập nhật CRM truyền ContactId vào Interaction")]
        public string WF_UpdateIC_Interaction { get; set; } = "";

        #endregion WF IBox

        #region Zip parth

        public string FileDay { get; set; } = "5";
        public string PathBackUp { get; set; } = "bak_bot";
        public string PathRestore { get; set; } = "";

        #endregion Zip parth
    }

    public enum TypeChat
    {
        ChatBot,
        ChatGoogle
    }
}