using IBox.Schedule.Library.Model;
using static IBox.Schedule.Library.ShrinkLogConnectionDB.ConnectionShrinkLog;

namespace IBox.Schedule.Library.ShrinkLogConnectionDB
{
    public interface IConnectionShrinkLog
    {
        bool CheckConnectSQL(DBConnectionShrinkLog dBConnection);

        List<LogSpaceInfoDatabase> GetLogSpaceInfoDatabase();

        List<Category> GetAllCategory();
    }
}