namespace IBox.Client.Business.Model
{
    public class ResponseDll
    {
        public string? name { get; set; }
        public string? id { get; set; }
        public DateTime? createdDate { get; set; }
        public bool isDelete { get; set; }
        public DateTime? modificationDate { get; set; }
        public List<BDynamicLinkedLibrary>? list { get; set; }
    }
}