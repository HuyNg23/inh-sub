namespace IBox.Schedule.SelfService.Model
{
    public class ScheduleInSite
    {
        private string site;
        private List<ScheduleInfo> jobSchedules;

        public string Site { get => site; set => site = value; }
        public List<ScheduleInfo> JobSchedules { get => jobSchedules; set => jobSchedules = value; }
    }
}
