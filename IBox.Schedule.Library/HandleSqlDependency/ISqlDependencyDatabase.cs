using IBox.Common.Model;

namespace IBox.Schedule.Library.HandleSqlDependency
{
    public interface ISqlDependencyDatabase
    {
        void OnLoadSqlDependency(TypeUserBase typeUserBase);
        void SqlDependencyStartService(string serviceName);
        void Dispose();
        void CleanupConversations(string queryRemoveEndpoints);
    }
}