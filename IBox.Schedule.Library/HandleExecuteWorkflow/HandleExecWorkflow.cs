using IBox.ChatBot.DB.DBChatDay;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Execution;
using IBox.Schedule.Library.Model;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using Quartz;
using Serilog;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using TypeScheduleBase = IBox.Database.Tenant.Tables.TypeScheduleBase;

namespace IBox.Schedule.Library.HandleExecuteWorkflow
{
    public class HandleExecWorkflow : IHandleExecWorkflow
    {
        private readonly IExecuteWF _executeWF;
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public HandleExecWorkflow(IExecuteWF executeWF, IConfiguration configuration, IEncryption encryption)
        {
            _executeWF = executeWF;
            _configuration = configuration;
            _encryption = encryption;
        }

        public void ExecWorkflow(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var wfid = dataMap.GetString("wfid") ?? string.Empty;
            var scheduleId = dataMap.GetString("scheduleId") ?? string.Empty;
            var tenantId = dataMap.GetString("tenantId") ?? string.Empty;
            var typeSchedule = dataMap.GetString("typeSchedule") ?? string.Empty;
            var jobKey = dataMap.GetString("jobKey") ?? string.Empty;
            var thisSite = IBGlobalConfig.ThisSite ?? string.Empty;
            var keyWF = Guid.NewGuid().ToString();

            var reqScheduleHistory = new ReqScheduleHistory()
            {
                KeyExecuteRunWorkFlow = keyWF,
                ScheduleId = scheduleId,
                Site = thisSite,
                TenantId = tenantId,
                Type = typeSchedule
            };

            DateTime CreateDate = DateTime.Now;

            try
            {
                Log.Information($"JobKey Execute: {jobKey}");

                if (string.IsNullOrEmpty(wfid))
                {
                    CreateUpdateScheduleHistory(reqScheduleHistory, StatusPlan.Fail, "Wfid input IsNullOrEmpty.", CreateDate, CreateDate);

                    Log.Error($"ExecWorkflow fail because wfid is null or empty.");

                    return;
                }

                var workflowExist = CheckWorkflowExist(tenantId, wfid);

                if (!workflowExist)
                {
                    CreateUpdateScheduleHistory(reqScheduleHistory, StatusPlan.Fail, $"Not found wfid: {wfid}", CreateDate, CreateDate);

                    Log.Error($"ExecWorkflow fail because not found wfid: {wfid}");

                    return;
                }

                var workflowIsDelete = CheckWorkflowIsDelete(tenantId, wfid);

                if (workflowIsDelete)
                {
                    CreateUpdateScheduleHistory(reqScheduleHistory, StatusPlan.Fail, $"Workflow with id: {wfid} has been deleted.", CreateDate, CreateDate);

                    Log.Error($"ExecWorkflow fail because wfid: {wfid} has been deleted.");

                    return;
                }

                ExecuteWorkflow(tenantId, wfid);
                DateTime modificationDate = DateTime.Now;
                CreateUpdateScheduleHistory(reqScheduleHistory, StatusPlan.Complete, "", CreateDate, modificationDate);
            }
            catch (Exception ex)
            {
                DateTime modificationDate = DateTime.Now;
                CreateUpdateScheduleHistory(reqScheduleHistory, StatusPlan.Fail, ex.ToString(), CreateDate, modificationDate);
                Log.Error("ExecWorkflow error because: " + ex.ToString());
            }
        }

        private bool CheckWorkflowExist(string tenantId, string wfid)
        {
            var rootContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

            var tenantContext = rootContext.GetTenantContext(tenantId).Context;

            var checkWfExist = tenantContext.WF_Defines.Where(ptr => ptr.Id == wfid).FirstOrDefault();

            tenantContext.Context.Dispose();

            if (checkWfExist != null)
            {
                return true;
            }

            return false;
        }

        private bool CheckWorkflowIsDelete(string tenantId, string wfid)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var checkWfExist = tenantContext.WF_Defines.Where(ptr => ptr.Id == wfid).FirstOrDefault();

            tenantContext.Context.Dispose();

            if (checkWfExist != null && checkWfExist.IsDelete)
            {
                return true;
            }

            return false;
        }

        private string CheckExistScheduleHistory(string tenantId, string keyWF)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var DataImplementationHistorys = tenantContext.S_ImplementationHistorys.FirstOrDefault(ptr => ptr.KeyExecuteRunWorkFlow == keyWF);

            if (DataImplementationHistorys != null)
            {
                return DataImplementationHistorys.Id;
            }

            tenantContext.Context.Dispose();

            return string.Empty;
        }

        private void CreateUpdateScheduleHistory(ReqScheduleHistory req, StatusPlan statusPlan, string reason, DateTime CreatedDate, DateTime ModificationDate)
        {
            List<S_ImplementationHistory> s_ImplementationHistories = new List<S_ImplementationHistory>()
            {
                 new S_ImplementationHistory()
                 {
                    CreatedDate = CreatedDate,
                    ModificationDate = ModificationDate,
                    ImplementationPlanID = "",
                    ScheduleID = req.ScheduleId,
                    StatusPlan = statusPlan,
                    Reason = reason,
                    Site = req.Site,
                    Type = (TypeScheduleBase)Enum.Parse(typeof(TypeScheduleBase), req.Type),
                    KeyExecuteRunWorkFlow = req.KeyExecuteRunWorkFlow,
                    IsDelete = false,
                 }
            };

            var (columns, values) = ExtractColumnsAndValues(s_ImplementationHistories);
            ExecuteMerge(
               req.TenantId,
               "S_ImplementationHistorys",
               "KeyExecuteRunWorkFlow",
               columns,
               values
            );

            //InsertOrUpdateImplementationHistoryWithRetry(s_ImplementationHistorys, tenantContext);
        }
        public (List<string> Columns, List<List<object>> Values) ExtractColumnsAndValues<T>(List<T> entities, params string[] excludeProperties)
        {
            var columns = new List<string>();
            var values = new List<List<object>>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !excludeProperties.Contains(p.Name)).ToList();

            columns = properties.Select(p => p.Name).ToList();

            foreach (var entity in entities)
            {
                var row = new List<object>();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(entity);
                    row.Add(value);
                }
                values.Add(row);
            }

            return (columns, values);
        }

        public void ExecuteMerge(
            string TenantId,
            string tableName,
            string keyColumn,
            List<string> columns,
            List<List<object>> values,
            int retryCount = 3,
            int delayMilliseconds = 1000)
        {

            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(TenantId).Context;

            var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
            var sourceColumns = columns;
            int attempt = 0;
            while (attempt < retryCount)
            {
                try
                {
                    using (var conn = tenantContext.Context.Database.GetDbConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        using (var command = conn.CreateCommand())
                        {
                            command.Transaction = transaction;
                            var query = new StringBuilder();
                            query.AppendLine($"MERGE INTO [{tableName}] WITH (UPDLOCK) AS target");
                            query.AppendLine("USING (VALUES");
                            var rowStrings = values.Select(row =>
                            {
                                var valStrs = row.Select(v =>
                                {
                                    return FormatSqlValue(v);
                                });
                                return $"({string.Join(", ", valStrs)})";
                            });

                            query.AppendLine(string.Join(",\n", rowStrings));
                            query.AppendLine(") AS source (" + columnList + ")");
                            query.AppendLine($"ON target.[{keyColumn}] = source.[{keyColumn}]");

                            if (columnList.Contains("StatusPlan"))
                            {
                                query.AppendLine("WHEN MATCHED AND (target.StatusPlan IS NULL OR source.StatusPlan > target.StatusPlan) THEN");
                                query.AppendLine("UPDATE SET");
                            }
                            else
                            {
                                query.AppendLine("WHEN MATCHED THEN UPDATE SET");
                            }

                            var updateSet = columns
                                .Where(c => !string.Equals(c, keyColumn, StringComparison.OrdinalIgnoreCase))
                                .Select(c =>
                                    $"{c} = COALESCE(NULLIF(source.{c}, ''), target.{c})");

                            query.AppendLine(string.Join(",\n", updateSet));
                            query.AppendLine("WHEN NOT MATCHED THEN");
                            query.AppendLine($"INSERT ({columnList})");
                            query.AppendLine($"VALUES ({string.Join(", ", sourceColumns.Select(c => $"source.{c}"))});");
                            command.CommandText = query.ToString();
                            command.ExecuteNonQuery();
                            transaction.Commit();
                            tenantContext.Context.Dispose();
                            break;
                        }
                    }
                }
                catch (SqlException ex) when (ex.Number == 1205)
                {
                    attempt++;
                    Log.Warning($"Deadlock detected on attempt {attempt}/{retryCount}. Retrying...");
                    if (attempt < retryCount)
                    {
                        Thread.Sleep(delayMilliseconds);
                    }
                    else
                    {
                        Log.Error($"Retry limit reached. Deadlock or other error occurred. {ex.Message}");
                        tenantContext.Context.Dispose();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while processing the MERGE operation. {ex.Message}");
                    tenantContext.Context.Dispose();
                    return;
                }
            }
        }
        private string FormatSqlValue(object v)
        {
            return v switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "1" : "0",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
                _ when v.GetType().IsEnum => ((int)v).ToString(),
                _ => $"'{v.ToString().Replace("'", "''")}'"
            };
        }
        public void InsertOrUpdateImplementationHistoryWithRetry(S_ImplementationHistory s_ImplementationHistorys, TenantContext tenantContext, int maxRetryCount = 3, int delayMilliseconds = 1000)
        {
            int attempt = 0;
            bool success = false;

            while (attempt < maxRetryCount && !success)
            {
                try
                {
                    using (var conn = tenantContext.Context.Database.GetDbConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        using (var command = conn.CreateCommand())
                        {
                            command.Transaction = transaction;

                            var query = new StringBuilder();
                            query.Append(@"
                        MERGE INTO S_ImplementationHistorys WITH (UPDLOCK) AS target
                        USING (
                            VALUES 
                    ");

                            var valueList = new List<string>();

                            var id = Guid.NewGuid();
                            var createdDate = s_ImplementationHistorys.CreatedDate ?? DateTime.Now;
                            var modifiedDate = s_ImplementationHistorys.ModificationDate ?? DateTime.Now;

                            valueList.Add(string.Format("('{0}', '{1}', '{2}', '{3}', '{4}', {5}, '{6}', '{7}', '{8}', {9}, '{10}')",
                                id,
                                ((int?)s_ImplementationHistorys.StatusPlan),
                                Escape(s_ImplementationHistorys.ScheduleID),
                                Escape(s_ImplementationHistorys.ImplementationPlanID),
                                Escape(s_ImplementationHistorys.Reason),
                                ((int?)s_ImplementationHistorys.Type),
                                Escape(s_ImplementationHistorys.Site),
                                Escape(s_ImplementationHistorys.KeyExecuteRunWorkFlow),
                                createdDate.ToString("yyyy-MM-dd HH:mm:ss"),
                                0,
                                modifiedDate.ToString("yyyy-MM-dd HH:mm:ss")));

                            query.AppendJoin(",", valueList);

                            query.Append(@"
                    ) AS source (
                        Id, StatusPlan, ScheduleID, ImplementationPlanID, Reason, Type, Site,
                        KeyExecuteRunWorkFlow, CreatedDate, IsDelete, ModificationDate
                    )
                    ON target.KeyExecuteRunWorkFlow = source.KeyExecuteRunWorkFlow
                    WHEN MATCHED THEN
                        UPDATE SET
                            StatusPlan = source.StatusPlan,
                            ScheduleID = source.ScheduleID,
                            ImplementationPlanID = source.ImplementationPlanID,
                            Reason = source.Reason,
                            Type = source.Type,
                            Site = source.Site,
                            CreatedDate = source.CreatedDate,
                            IsDelete = source.IsDelete,
                            ModificationDate = source.ModificationDate
                    WHEN NOT MATCHED THEN
                        INSERT (
                            Id, StatusPlan, ScheduleID, ImplementationPlanID, Reason, Type, Site,
                            KeyExecuteRunWorkFlow, CreatedDate, IsDelete, ModificationDate
                        )
                        VALUES (
                            source.Id, source.StatusPlan, source.ScheduleID, source.ImplementationPlanID,
                            source.Reason, source.Type, source.Site, source.KeyExecuteRunWorkFlow,
                            source.CreatedDate, source.IsDelete, source.ModificationDate
                        );");

                            command.CommandText = query.ToString();
                            command.ExecuteNonQuery();
                            transaction.Commit();

                            success = true;
                        }
                    }
                }
                catch (SqlException ex) when (ex.Number == 1205)
                {
                    attempt++;
                    if (attempt < maxRetryCount)
                    {
                        Thread.Sleep(delayMilliseconds);
                    }
                    else
                    {
                        throw new Exception("Retry limit reached. Deadlock or other error occurred.", ex);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while processing the MERGE operation.", ex);
                }
            }
        }
        private static string Escape(string? value)
        {
            return (value ?? "").Replace("'", "''");
        }
        private void ExecuteWorkflow(string tenantId, string wfid)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            _executeWF.SetTenantContext(tenantContext);

            _executeWF.Execute(wfid, "{}", tenantId, false);

            tenantContext.Context.Dispose();
        }
    }
}