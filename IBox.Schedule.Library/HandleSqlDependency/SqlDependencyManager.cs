using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.Schedule.Library.HandleSqlDependency
{
    public class SqlDependencyManager: IDisposable
    {
        private readonly string _connectionString;
        private static bool _isStarted = false;
        private static Dictionary<string, SqlDependency> _dependencies = new Dictionary<string, SqlDependency>();

        public SqlDependencyManager(string connectionString)
        {
            _connectionString = connectionString;
            Start();
        }

        private void Start()
        {
            if (!_isStarted)
            {
                SqlDependency.Start(_connectionString);
                _isStarted = true;
                Log.Information("SqlDependency started.");
            }
        }

        public void RegisterListener(string tableName, string query, OnChangeEventHandler callback)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        var dependency = new SqlDependency(cmd);

                        if (_dependencies.ContainsKey(tableName))
                        {
                            _dependencies[tableName].OnChange -= callback;
                            _dependencies[tableName] = dependency;
                        }
                        else
                        {
                            _dependencies.Add(tableName, dependency);
                        }

                        dependency.OnChange += (sender, e) =>
                        {
                            try
                            {
                                callback(sender, e);
                                RegisterListener(tableName, query, callback);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"OnChange event error: {ex}");
                            }
                        };

                        cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RegisterListener error: {ex}");
            }
        }

        public void Dispose()
        {
            if (_isStarted)
            {
                foreach (var dep in _dependencies)
                {
                    dep.Value.OnChange -= null;
                }

                _dependencies.Clear();
                SqlDependency.Stop(_connectionString);
                _isStarted = false;
                Log.Information($"SqlDependency stopped. connect {_connectionString}");
            }
        }
    }
}
