namespace WebAPI.Model.IC
{
    public class GetListHistory
    {
        public string? app_id { get; set; }
        public string? user_id_by_app { get; set; }
        public string? username { get; set; }
        public string? channel { get; set; }
        public string? description { get; set; }
        public string? notes { get; set; }
    }

    public class ResponseGetListHistory
    {
        public string? app_id { get; set; }
        public string? user_id_by_app { get; set; }
        public string? username { get; set; }
        public List<data2>? Data { get; set; }
        public string? statecode { get; set; }
        public int? error { get; set; }
        public string? message { get; set; }
    }

    public class data2
    {
        public string? msg_type { get; set; }
        public string? author_type { get; set; }
        public string? msg_id { get; set; }
        public string? text { get; set; }
        public string? file_url { get; set; }
        public string? file_type { get; set; }
        public string? location_lat { get; set; }
        public string? location_long { get; set; }
        public string? timestamp { get; set; }
    }
}