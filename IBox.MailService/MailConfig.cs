namespace IBox.MailService
{
    public class MailConfig
    {
        private string? mailServerID;
        private string? mailTo;
        private string? mailCC;
        private string? mailBCC;
        private string? mailTitle;
        private string? mailBody;

        public string? MailServerID { get => mailServerID; set => mailServerID = value; }
        public string? MailTo { get => mailTo; set => mailTo = value; }
        public string? MailCC { get => mailCC; set => mailCC = value; }
        public string? MailBCC { get => mailBCC; set => mailBCC = value; }
        public string? MailTitle { get => mailTitle; set => mailTitle = value; }
        public string? MailBody { get => mailBody; set => mailBody = value; }
    }
}