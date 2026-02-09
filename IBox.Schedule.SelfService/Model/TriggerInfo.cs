namespace IBox.Schedule.SelfService.Model
{
    public class TriggerInfo
    {
        private string name;
        private string group;
        private string cronExpression;
        private string nextFireTime;
        private string previousFireTime;
        private string status;
        private string result;

        public string Name { get => name; set => name = value; }
        public string Group { get => group; set => group = value; }
        public string CronExpression { get => cronExpression; set => cronExpression = value; }
        public string NextFireTime { get => nextFireTime; set => nextFireTime = value; }
        public string PreviousFireTime { get => previousFireTime; set => previousFireTime = value; }
        public string Status { get => status; set => status = value; }
        public string Result { get => result; set => result = value; }
    }
}
