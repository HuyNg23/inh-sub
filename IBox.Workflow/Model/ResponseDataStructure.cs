using IBox.Database.Tenant.Tables;

namespace IBox.Workflow.Model
{
    public class ResponseDataStructure
    {
        public string? name { get; set; }
        public string? id { get; set; }
        public DateTime? createdDate { get; set; }
        public bool isDelete { get; set; }
        public DateTime? modificationDate { get; set; }
        public List<Obj>? list { get; set; }
    }
}