using IBox.Schedule.ShrinkLog.Model;
using static IBox.Schedule.ShrinkLog.ConnectionDB.ConnectionShrinkLog;

namespace IBox.Schedule.ShrinkLog.ConnectionDB
{
    public interface IConnectionShrinkLog
    {
        bool CheckConnectSQL(DBConnectionShrinkLog dBConnection);
        List<LogSpaceInfoDatabase> GetLogSpaceInfoDatabase();
        List<Category> GetAllCategory();
    }
}
