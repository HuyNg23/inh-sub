using IBox.Workflow.Model;
using System.Collections.Concurrent;

namespace IBox.Workflow.Execution
{
    public static class ListTenantWF
    {
        public static ConcurrentDictionary<string, StaticListWF> TenantWFs = new ConcurrentDictionary<string, StaticListWF>();
    }
    public class StaticListWF
    {
        public ConcurrentDictionary<string, WFDefine> WFs = new ConcurrentDictionary<string, WFDefine>();
        public ConcurrentDictionary<string, Type> ObjSchemaes = new ConcurrentDictionary<string, Type>();
    }
}