namespace IBox.Security
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class IBoxActionPermissionAttribute : Attribute
    {
        public List<string> ActionNames { get; }

        public IBoxActionPermissionAttribute(params string[] actionNames)
        {
            ActionNames = actionNames.ToList();
        }
    }
}