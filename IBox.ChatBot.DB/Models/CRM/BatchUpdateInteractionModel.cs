namespace IBox.ChatBot.DB.Models.CRM
{
    public class BatchUpdateInteractionModel
    {
        public string? tag { get; set; }
        public string? customerName { get; set; }
        public string? senderId { get; set; }
        public string? sessionId { get; set; }
        public string? fld_closed_by_01 { get; set; }
        public string? fld_interaction_channel_00000001 { get; set; }
        public string? fld_interaction_source_contact_id_00000001 { get; set; }
        public string? fld_interaction_sub_channel_00000001 { get; set; }
        public string? fld_sender_id_fpt_01 { get; set; }
        public string? fld_tenkh_43688483 { get; set; }
        public string? fld_lich_su_doan_chat_01 { get; set; }
        public string? _id { get; set; }
    }

    public class LstData
    {
        public List<BatchUpdateInteractionModel>? Lst_Data { get; set; }
    }
}