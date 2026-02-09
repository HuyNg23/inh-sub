namespace IBox.Schedule.SelfService.Model
{
    public class RequestAllImplementationPlansDuringDay
    {
        private string? textSearch;
        private int page;

        public string? TextSearch { get => textSearch; set => textSearch = value; }
        public int PageNum { get => page; set => page = value; }
    }
}
