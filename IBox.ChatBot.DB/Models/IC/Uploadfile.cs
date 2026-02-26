namespace WebAPI.Model.IC
{
    public class Uploadfile
    {
        public string? app_id { get; set; }
        public string? type { get; set; }
        public string? file_url { get; set; }
        public string? username { get; set; }
        public string? user_id_by_app { get; set; }
    }

    public class data1
    {
        public string? file_id { get; set; }
    }

    public class ReponeUploadfile
    {
        public string? statecode { get; set; }
        public int error { get; set; }
        public string? message { get; set; }
        public data1? data { get; set; }
    }

    public class ReponeUploadfileBOT
    {
        public string? ok { get; set; }
        public string? url { get; set; }
    }
}