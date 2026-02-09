using IBox.Common.Model;
using IBox.Database.Root;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Schedule.Library.HubSchedule;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.Schedule.Library.Execution
{
    [DisallowConcurrentExecution]
    public class ExecuteClearEndpoints : IJob
    {
        private readonly ISqlDependencyDatabase _sqlDependencyDatabase;
        public ExecuteClearEndpoints(ISqlDependencyDatabase sqlDependencyDatabase)
        {
            _sqlDependencyDatabase = sqlDependencyDatabase;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (_sqlDependencyDatabase is SqlDependencyDatabase db)
                {
                    db.DisableDispose(); // Hàm này sẽ tắt Dispose
                }

                var urlClientBe = IBGlobalConfig.ServersIBox.OrderBy(x => x).FirstOrDefault();
                if (urlClientBe == null || (urlClientBe != null && !IBGlobalConfig.ThisSite.Contains(urlClientBe)))
                {
                    return Task.CompletedTask;
                }

                string queryRemoveEndpoints = @"
                   DECLARE @convHandle UNIQUEIDENTIFIER;
                   DECLARE conv_cursor CURSOR FOR 
                    WITH CTE AS (
                        SELECT 
                            conversation_handle, 
                            far_service, 
                            lifetime,
                            state,
                            ROW_NUMBER() OVER (PARTITION BY far_service ORDER BY lifetime DESC) AS rn
                        FROM sys.conversation_endpoints
                        WHERE service_id NOT IN (1)
                    )
                    SELECT conversation_handle 
                    FROM CTE 
                    WHERE rn > 1 OR state = 'CD';
                    OPEN conv_cursor;
                    FETCH NEXT FROM conv_cursor INTO @convHandle;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        END CONVERSATION @convHandle WITH CLEANUP;

                        FETCH NEXT FROM conv_cursor INTO @convHandle;
                    END
                    CLOSE conv_cursor;
                    DEALLOCATE conv_cursor;";
                _sqlDependencyDatabase.CleanupConversations(queryRemoveEndpoints);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error($"EndChatAll: {ex.Message}");
                throw;
            }
        }
    }
}
