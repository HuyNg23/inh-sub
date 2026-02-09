namespace IBox.Common.Objects
{
    public class RequestForm<TIn>
    {
        private string? user;
        private readonly DateTime requestedDate = DateTime.Now;
        private TIn? body;

        public string? User { get => user; set => user = value; }
        public DateTime? RequestedDate { get => requestedDate; }

        public TIn Body
        {
            get
            {
                if (body == null)
                {
                    var instance = Activator.CreateInstance(typeof(TIn));
                    if (instance != null)
                    {
                        return (TIn)instance;
                    }
                }


                return body;

            }
            set => body = value;
        }
    }
}