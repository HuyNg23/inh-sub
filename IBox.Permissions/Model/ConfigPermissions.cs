namespace IBox.Permissions.Model
{
    public class ConfigPermissions
    {
        public string RoleId { get; set; } = string.Empty;

        public List<string> PermissionsKey { get; set; } = new List<string>();
    }
}