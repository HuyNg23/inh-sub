using IBox.Database.Tenant.Tables;

namespace IBox.Documentation.Models
{
    public class BackUpAndRestore
    {
        public List<WF_Define>? WF_Define { get; set; }
        public List<WF_Step>? WF_Steps { get; set; }
        public List<DBStructure>? DBStructure { get; set; }
        public List<Obj>? Objs { get; set; }
        public List<Obj_Schema>? Obj_Schemas { get; set; }
        public List<WF_Step_Edge>? WF_Step_Edges { get; set; }
    }
}