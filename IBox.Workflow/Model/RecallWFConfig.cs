namespace IBox.Workflow.Model
{
    public class RecallWFConfig
    {
        private string? wfid;
        private string? bodyRequest;

        public string? Wfid { get => wfid; set => wfid = value; }
        public string? BodyRequest { get => bodyRequest; set => bodyRequest = value; }
    }
}