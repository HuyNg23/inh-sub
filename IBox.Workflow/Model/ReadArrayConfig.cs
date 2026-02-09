namespace IBox.Workflow.Model
{
    public class ReadArrayConfig
    {
        private string? fieldName;
        private string? wfid;
        private RunType? runType;

        public string? FieldName { get => fieldName; set => fieldName = value; }
        public string? Wfid { get => wfid; set => wfid = value; }
        public RunType? RunType { get => runType; set => runType = value; }
    }

    public enum RunType
    {
        sync,
        asyncs
    }
}