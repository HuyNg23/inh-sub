namespace IBox.DLEx.Model
{
    public class RecallWfConfig
    {
        private string? wfid;
        private string? bodyRequest;

        public string? Wfid { get => wfid; set => wfid = value; }
        public string? BodyRequest { get => bodyRequest; set => bodyRequest = value; }
    }
}