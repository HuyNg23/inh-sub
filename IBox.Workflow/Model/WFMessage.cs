namespace IBox.Workflow.Model
{
    public class WFMessage
    {
        private string? code;
        private string? message;
        private WFMessage? innerMessage;


        public string Code { get => code; set => code = value; }


        public string Message { get => message; set => message = value; }


        public WFMessage InnerMessage { get => innerMessage; set => innerMessage = value; }

    }
}