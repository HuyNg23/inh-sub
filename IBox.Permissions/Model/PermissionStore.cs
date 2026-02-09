using System.Collections.ObjectModel;

namespace IBox.Permissions.Model
{
    public class Permission
    {
        public string Id { get; set; } = string.Empty;
        public string Menu { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Function { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public static class PermissionStore
    {
        private static readonly Dictionary<string, Permission> _permissions = new Dictionary<string, Permission>()
        {
            { "Home-HistoryAPI-View", new Permission { Menu = "Home", Controller = "", Function = "HistoryAPI", Action = "View", Type = "TENANT" } },
            { "Home-HistoryWorkflow-View", new Permission { Menu = "Home", Controller = "", Function = "HistoryAPI", Action = "View", Type = "TENANT" } },
            { "Home-HistorySchedule-View", new Permission { Menu = "Home", Controller = "", Function = "HistoryAPI", Action = "View", Type = "TENANT" } },
            { "Home-SizeFolderChatBot-View", new Permission { Menu = "Home", Controller = "", Function = "HistoryAPI", Action = "View", Type = "TENANT" } },

            { "RequestTool-Rest-GetAll-GetAll", new Permission { Menu = "RequestTool", Controller = "Rest", Function = "GetAll", Action = "GetAll", Type = "TENANT" } },
            { "RequestTool-Rest-GetDetail-GetDetail", new Permission { Menu = "RequestTool", Controller = "Rest", Function = "GetDetail", Action = "GetDetail", Type = "TENANT" } },
            { "RequestTool-Rest-Create-Create", new Permission { Menu = "RequestTool", Controller = "Rest", Function = "Create", Action = "Create", Type = "TENANT" } },
            { "RequestTool-Rest-Update-Update", new Permission { Menu = "RequestTool", Controller = "Rest", Function = "Update", Action = "Update", Type = "TENANT" } },
            { "RequestTool-Rest-Delete-Delete", new Permission { Menu = "RequestTool", Controller = "Rest", Function = "Delete", Action = "Delete", Type = "TENANT" } },

            { "SystemConfig-XRole-CustomizeRoles-View", new Permission { Menu = "SystemConfig", Controller = "XRole", Function = "CustomizeRoles", Action = "View", Type = "TENANT" } },
            { "SystemConfig-XRole-CustomizeRoles-Create", new Permission { Menu = "SystemConfig", Controller = "XRole", Function = "CustomizeRoles", Action = "Create", Type = "TENANT" } },
            { "SystemConfig-XRole-CustomizeRoles-Update", new Permission { Menu = "SystemConfig", Controller = "XRole", Function = "CustomizeRoles", Action = "Update", Type = "TENANT" } },
            { "SystemConfig-XRole-CustomizeRoles-Delete", new Permission { Menu = "SystemConfig", Controller = "XRole", Function = "CustomizeRoles", Action = "Delete", Type = "TENANT" } },
            { "SystemConfig-XRole-CustomizeRoles-Config", new Permission { Menu = "SystemConfig", Controller = "XRole", Function = "CustomizeRoles", Action = "Config", Type = "TENANT" } },
        };

        public static IReadOnlyDictionary<string, Permission> Permissions { get; } = new ReadOnlyDictionary<string, Permission>(_permissions);

        public static Permission GetPermission(string id)
        {
            _permissions.TryGetValue(id, out var permission);
            return permission ?? new Permission();
        }
    }
}