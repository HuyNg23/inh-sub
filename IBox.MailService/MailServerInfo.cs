namespace IBox.MailService
{
    public class MailServerInfo
    {
        private string? id;
        private string? smtp_Host;
        private string? smtp_Port;
        private string? smtp_Username;
        private string? smtp_Password;
        private bool? smtp_Credentials;
        private bool? smtp_EnableSSL;

        public string? Id { get => id; set => id = value; }
        public string? Smtp_Host { get => smtp_Host; set => smtp_Host = value; }
        public string? Smtp_Port { get => smtp_Port; set => smtp_Port = value; }
        public string? Smtp_Username { get => smtp_Username; set => smtp_Username = value; }
        public string? Smtp_Password { get => smtp_Password; set => smtp_Password = value; }
        public bool? Smtp_Credentials { get => smtp_Credentials; set => smtp_Credentials = value; }
        public bool? Smtp_EnableSSL { get => smtp_EnableSSL; set => smtp_EnableSSL = value; }
    }
}