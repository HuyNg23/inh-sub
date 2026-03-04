using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.ConsistentHashing;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace IBox.ChatBot.HandleScheduleSQL
{
    public class CreateDB : ICreateDB
    {
        private readonly ICommonData _commonData;
        private readonly DBChatDayContextFactory _contextFactory;
        private readonly DBHistoryContextFactory _historyContextFactory;

        public CreateDB(ICommonData commonData, DBChatDayContextFactory contextFactory, DBHistoryContextFactory historyContextFactory)
        {
            _commonData = commonData;
            _contextFactory = contextFactory;
            _historyContextFactory = historyContextFactory;
        }

        public void CreateDBLiteChatBot(string tenantId)
        {
            try
            {
                string pathNow = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseChatBot, tenantId);
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return;
                }

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 1; j <= 100; j++)
                    {
                        string fileDBChatBotNow1 = GetFilePath(pathNow, DateTime.Now.AddDays(i));
                        _commonData.CreateFolder1(fileDBChatBotNow1);
                        string PathdbDayNow1 = Path.Combine(fileDBChatBotNow1, $"chatbot_{j}.db");
                        if (File.Exists(PathdbDayNow1))
                        {
                            using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayNow1))
                            {
                                using (var connection = dbContextDaily.Database.GetDbConnection())
                                {
                                    connection.Open();
                                    var command = connection.CreateCommand();

                                    command.CommandText = $"PRAGMA table_info(ChatSessionDailies);";
                                    var existingColumns = new List<string>();

                                    using (var reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            existingColumns.Add(reader["name"].ToString());
                                        }
                                    }

                                    var columns = _commonData.GetColumnsFromModelSQLite<ChatSessionDaily>();
                                    foreach (var column in columns)
                                    {
                                        if (!existingColumns.Contains(column.Key))
                                        {
                                            string querry = $"ALTER TABLE ChatSessionDailies ADD COLUMN {column.Key} {column.Value};";
                                            ExecuteSQLite(command, querry);
                                        }
                                    }
                                }

                                using (var connection = dbContextDaily.Database.GetDbConnection())
                                {
                                    connection.Open();
                                    var command = connection.CreateCommand();
                                    command.CommandText = $"PRAGMA table_info(ChatMessageDailies);";
                                    var existingColumns = new List<string>();

                                    using (var reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {

                                            existingColumns.Add(reader["name"].ToString());

                                        }
                                    }

                                    var columns = _commonData.GetColumnsFromModelSQLite<ChatMessageDaily>();
                                    foreach (var column in columns)
                                    {
                                        if (!existingColumns.Contains(column.Key))
                                        {
                                            string querry = $"ALTER TABLE ChatMessageDailies ADD COLUMN {column.Key} {column.Value};";
                                            ExecuteSQLite(command, querry);
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayNow1))
                        {
                            dbContextDaily.Database.EnsureCreated();
                        }
                    }


                    string fileDBChatBotNow = GetFilePath(pathNow, DateTime.Now.AddDays(-i));

                    if (!Directory.Exists(fileDBChatBotNow))
                    {
                        continue;
                    }

                    var dbFiles = Directory.GetFiles(fileDBChatBotNow, "*.db", SearchOption.TopDirectoryOnly).ToList();
                    foreach (var pathFile in dbFiles)
                    {
                        //string PathdbDayNow = Path.Combine(pathFile, $"chatbot.db");
                        string PathdbDayNow = pathFile;
                        if (File.Exists(PathdbDayNow))
                        {
                            using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayNow))
                            {
                                using (var connection = dbContextDaily.Database.GetDbConnection())
                                {
                                    connection.Open();
                                    var command = connection.CreateCommand();

                                    command.CommandText = $"PRAGMA table_info(ChatSessionDailies);";
                                    var existingColumns = new List<string>();

                                    using (var reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {

                                            existingColumns.Add(reader["name"].ToString());

                                        }
                                    }

                                    var columns = _commonData.GetColumnsFromModelSQLite<ChatSessionDaily>();
                                    foreach (var column in columns)
                                    {
                                        if (!existingColumns.Contains(column.Key))
                                        {
                                            string querry = $"ALTER TABLE ChatSessionDailies ADD COLUMN {column.Key} {column.Value};";
                                            ExecuteSQLite(command, querry);
                                        }
                                    }
                                }

                                using (var connection = dbContextDaily.Database.GetDbConnection())
                                {
                                    connection.Open();
                                    var command = connection.CreateCommand();
                                    command.CommandText = $"PRAGMA table_info(ChatMessageDailies);";
                                    var existingColumns = new List<string>();

                                    using (var reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {

                                            existingColumns.Add(reader["name"].ToString());

                                        }
                                    }

                                    var columns = _commonData.GetColumnsFromModelSQLite<ChatMessageDaily>();
                                    foreach (var column in columns)
                                    {
                                        if (!existingColumns.Contains(column.Key))
                                        {
                                            string querry = $"ALTER TABLE ChatMessageDailies ADD COLUMN {column.Key} {column.Value};";
                                            ExecuteSQLite(command, querry);
                                        }
                                    }
                                }
                            }
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateDBLiteChatBot: {ex.Message} \n tenantId: {tenantId}");
            }
        }

        public void CreateDBSQLiteHistory(string tenantId)
        {
            try
            {
                string pathNow = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseLogIBox, tenantId);
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return;
                }

                for (int i = 0; i < 3; i++)
                {
                    string fileDBChatBotNow1 = GetFilePath(pathNow, DateTime.Now.AddDays(i));
                    _commonData.CreateFolder1(fileDBChatBotNow1);
                    string PathdbDayNow1 = Path.Combine(fileDBChatBotNow1, $"history.db");
                    if (File.Exists(PathdbDayNow1))
                    {
                        using (var dbContextDailyHistory = _historyContextFactory.CreateContext(PathdbDayNow1))
                        {
                            using (var connection = dbContextDailyHistory.Database.GetDbConnection())
                            {
                                connection.Open();
                                var command = connection.CreateCommand();

                                command.CommandText = $"PRAGMA table_info(H_ApiThirdPartyExecuteHistories);";
                                var existingColumns = new List<string>();

                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {

                                        existingColumns.Add(reader["name"].ToString());

                                    }
                                }

                                var columns = _commonData.GetColumnsFromModelSQLite<H_ApiThirdPartyExecuteHistory>();
                                foreach (var column in columns)
                                {
                                    if (!existingColumns.Contains(column.Key))
                                    {
                                        string querry = $"ALTER TABLE H_ApiThirdPartyExecuteHistories ADD COLUMN {column.Key} {column.Value};";
                                        ExecuteSQLite(command, querry);
                                    }
                                }
                            }

                            using (var connection = dbContextDailyHistory.Database.GetDbConnection())
                            {
                                connection.Open();
                                var command = connection.CreateCommand();
                                command.CommandText = $"PRAGMA table_info(H_WorkflowExecuteHistories);";
                                var existingColumns = new List<string>();

                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {

                                        existingColumns.Add(reader["name"].ToString());

                                    }
                                }

                                var columns = _commonData.GetColumnsFromModelSQLite<H_WorkflowExecuteHistory>();
                                foreach (var column in columns)
                                {
                                    if (!existingColumns.Contains(column.Key))
                                    {
                                        string querry = $"ALTER TABLE H_WorkflowExecuteHistories ADD COLUMN {column.Key} {column.Value};";
                                        ExecuteSQLite(command, querry);
                                    }
                                }
                            }
                        }

                        continue;
                    }

                    using (var dbContextDaily = _historyContextFactory.CreateContext(PathdbDayNow1))
                    {
                        dbContextDaily.Database.EnsureCreated();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateDBLiteHistory: {ex.Message} \n tenantId: {tenantId}");
            }
        }

        public void ExecuteSQLite(DbCommand command, string querry)
        {
            command.CommandText = querry;
            command.ExecuteNonQuery();
        }

        private bool IsSafeColumnName(string columnName)
        {
            return !string.IsNullOrEmpty(columnName) && columnName.All(char.IsLetterOrDigit);
        }

        private bool IsSafeDataType(string dataType)
        {
            var allowedTypes = new List<string> { "TEXT", "INTEGER", "REAL", "DATETIME", "DECIMAL" };
            return allowedTypes.Contains(dataType.ToUpper());
        }

        public string GetFilePath(string Parth, DateTime messageDate)
        {
            return Path.Combine(Parth,
                                messageDate.ToString("yyyy"),
                                messageDate.ToString("MM"),
                                messageDate.ToString("dd"));
        }

        /// <summary>
        /// Tạo db sqlite history theo đường dẫn
        /// </summary>
        /// <param name="pathNow">...\IBox.LogService\DatabaseLogIBox\ea07ea12-13ec-4ce9-8cd6-44cd27287fff\2025\03\10</param>
        /// <returns></returns>
        public string CreateDBSQLiteHistorySizeTime(string pathNow, DateTime dateTime)
        {
            try
            {
                string[] dbFiles = Directory.GetFiles(pathNow, "history*.db");

                if (dbFiles.Length == 0)
                {
                    return CreateDBSQLite(pathNow);
                }

                Regex dbPattern = new(@"history_(\d{8}_\d{6})\.db$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                var sortedFiles = dbFiles
                    .Select(file => new
                    {
                        Path = file,
                        Match = dbPattern.Match(file),
                        IsDefault = Path.GetFileName(file).Equals("history.db", StringComparison.OrdinalIgnoreCase)
                    })
                    .Select(f => new
                    {
                        f.Path,
                        f.IsDefault,
                        Date = f.Match.Success ? DateTime.ParseExact(f.Match.Groups[1].Value, "yyyyMMdd_HHmmss", null) : DateTime.MinValue
                    })
                    .OrderBy(f => f.IsDefault)
                    .ThenByDescending(f => f.Date)
                    .ToList();

                if (new FileInfo(sortedFiles.First().Path).Length / 1024.0 > 1000)
                {
                    CreateDBSQLite(pathNow);
                }

                var validFiles = sortedFiles
                    .Where(f => f.IsDefault || f.Date <= dateTime)
                    .Take(10)
                    .Select(f => f.Path)
                    .ToList();

                return validFiles.FirstOrDefault() ?? "";
            }
            catch (Exception ex)
            {
                Log.Error($"CreateDBSQLiteHistorySize: {ex} \n pathNow: {pathNow}");
                return "";
            }
        }
        public string CreateDBSQLiteHistorySizeSTT(string pathNow)
        {
            try
            {
                var baseName = Path.GetFileNameWithoutExtension(pathNow);
                var regex = new Regex(@"^(chatbot_\d+)", RegexOptions.None, TimeSpan.FromSeconds(2));
                var match = regex.Match(baseName);
                baseName = match.Success ? match.Value : baseName;
                string currentFile = Path.Combine(Path.GetDirectoryName(pathNow), baseName + ".db");

                if (!File.Exists(currentFile))
                {
                    File.Create(currentFile).Dispose();
                    return currentFile;
                }

                FileInfo fileInfo = new FileInfo(currentFile);
                if (fileInfo.Length / 1024.0 < 1000)
                    return currentFile;

                int shardIndex = 1;
                while (true)
                {
                    string shardFile = Path.Combine(Path.GetDirectoryName(pathNow), $"{baseName}_{shardIndex}.db");

                    if (!File.Exists(shardFile))
                    {
                        // Tạo shard mới
                        File.Create(shardFile).Dispose();
                        return shardFile;
                    }

                    FileInfo shardInfo = new FileInfo(shardFile);
                    if (fileInfo.Length / 1024.0 < 1000)
                        return shardFile;

                    shardIndex++;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateDBSQLiteHistorySizeSTT: {ex} \n pathNow: {pathNow}");
                return "";
            }
        }

        public string CreateDBSQLite(string path)
        {
            try
            {
                DateTime newTime = DateTime.Now.AddMinutes(1);
                string newFileName = $"history_{newTime.ToString("yyyyMMdd_HHmm00")}.db";
                string newFilePath = Path.Combine(path, newFileName);

                if (!File.Exists(newFilePath))
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(newFilePath))
                    {
                        dbContextDaily.Database.EnsureCreated();
                    }
                }

                return newFilePath;
            }
            catch (Exception ex)
            {
                Log.Error($"CreateDBSQLite: {ex.Message}");
                return "";
            }
        }
    }
}