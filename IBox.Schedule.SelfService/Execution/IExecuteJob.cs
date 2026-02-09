namespace IBox.Schedule.SelfService.Execution
{
    public interface IExecuteJob
    {
        public void RunJobDay();
        public void RunJobServer(string serverNameError, string serverNameRun);
    }
}
