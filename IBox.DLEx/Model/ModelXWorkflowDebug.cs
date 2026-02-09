namespace IBox.DLEx.Model
{
    public class ModelXWorkflowDebug
    {
        private object? requestBody;
        private object? responseBody;
        private string? stepID;
        private string? stepName;
        private string? errorMessage;
        private DateTime? startDate = DateTime.Now;
        private string? workflowId;

        public object? RequestBody { get => requestBody; set => requestBody = value; }
        public object? ResponseBody { get => responseBody; set => responseBody = value; }
        public string? StepID { get => stepID; set => stepID = value; }
        public string? StepName { get => stepName; set => stepName = value; }
        public string? ErrorMessage { get => errorMessage; set => errorMessage = value; }
        public DateTime? StartDate { get => startDate; set => startDate = value; }
        public string? WorkflowId { get => workflowId; set => workflowId = value; }
    }
}