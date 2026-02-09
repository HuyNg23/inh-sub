using IBox.Database.DBC.Implementation;
using IBox.Database.Tenant.Tables;

namespace IBox.Database.DBC.Factories
{
    public interface IDBConnection : IDisposable
    {
        DBConnection SetConnection(DatabaseConnection connection);

        DBConnection SetConnection(string connection);
    }
}