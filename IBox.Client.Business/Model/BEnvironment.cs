using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.RegularExpressions;

namespace IBox.Client.Business.Model
{
    internal class Environment
    {
        private string name = string.Empty;
        private bool state = false;

        public string Name { get => name; set => name = value; }
        public bool State { get => state; set => state = value; }
    }

    public class BEnvironment : BBaseBusiness<BEnvironment>
    {
        public class BEnvironmentBuilder : BBaseBusiness<BEnvironmentBuilder>
        {
            private string _IBOXAdmin = string.Empty;
            private string _IBOXAdminPassword = string.Empty;

            public string IBOXAdmin { get => _IBOXAdmin; set => _IBOXAdmin = value; }
            public string IBOXAdminPassword { get => _IBOXAdminPassword; set => _IBOXAdminPassword = value; }

            public void ConfigRootDatabase(BEnvironmentBuilder payLoad)
            {
                try
                {
                    CheckIsValidField(payLoad);
                    CreateDatabase();
                    CreateIBoxAdmin();
                    CreateStoreListenStatusServerSchedule();
                    CreateStoreListenStatusServerShrinkLog();
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
                }
            }

            private void CheckIsValidField(BEnvironmentBuilder payLoad)
            {
                int minLength = 8;
                int maxLengthUser = 16;
                string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$";

                if (string.IsNullOrEmpty(payLoad.IBOXAdmin))
                {
                    throw new IboxLog("User Admin cannot be empty.", "AppLogs");
                }

                if (payLoad.IBOXAdmin.Length < minLength || payLoad.IBOXAdmin.Length > maxLengthUser)
                {
                    throw new IboxLog("User Admin must be 8 - 16 characters.", "AppLogs");
                }

                if (string.IsNullOrEmpty(payLoad.IBOXAdminPassword))
                {
                    throw new IboxLog("Password Admin cannot be empty.", "AppLogs");
                }

                if (!Regex.IsMatch(payLoad.IBOXAdminPassword, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                {
                    throw new IboxLog("Password Admin must 8 - 32 characters, contain at least one uppercase letter, one lowercase letter, one number and one special character.", "AppLogs");
                }
            }

            // do something about create or init environment
            // do something about implementation or intant database or account or create something

            private AdminState CreateIBoxAdmin()
            {
                try
                {
                    var isDatabaseConnected = this.RootContext.Context.Database.CanConnect();
                    if (isDatabaseConnected)
                    {
                        var userRootExist = this.RootContext.Context.U_Users.Where(user => user.RoleID == "ROOT" && user.TenantID == "ROOT").Any();

                        if (userRootExist)
                        {
                            throw new IboxLog("There is an admin account already created.", "AppLogs");
                        }

                        var userAdmin = this.RootContext.Context.U_Users.FirstOrDefault(user => user.UserName == this._IBOXAdmin);

                        if (userAdmin == null)
                        {
                            userAdmin.TryCreate(this.RootContext.Context, new U_User()
                            {
                                UserName = this._IBOXAdmin,
                                Password = this.Encryption.SHAEncode(this._IBOXAdminPassword),
                                RoleID = "ROOT",
                                TenantID = "ROOT"
                            });
                        }
                    }

                    var userAdminExist = this.RootContext.Context.U_Users.FirstOrDefault(user => user.UserName == this._IBOXAdmin);
                    if (userAdminExist == null)
                    {
                        return AdminState.CreateFailed;
                    }

                    return AdminState.CreateSuccess;
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
                }
            }

            private DatabaseState CreateDatabase()
            {
                try
                {
                    var isDatabaseConnected = this.RootContext.Context.Database.CanConnect();
                    if (!isDatabaseConnected)
                    {
                        this.RootContext.Context.Database.EnsureCreated();
                    }
                    else
                    {
                        return DatabaseState.ReadyToUse;
                    }

                    Thread.Sleep(3000);

                    isDatabaseConnected = this.RootContext.Context.Database.CanConnect();
                    if (!isDatabaseConnected)
                    {
                        return DatabaseState.CreateFailed;
                    }

                    return DatabaseState.CreateSuccess;
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
                }
            }

            private void CreateStoreListenStatusServerSchedule()
            {
                try
                {
                    string query = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateStatusServerSchedule]') AND type in (N'P'))
								BEGIN
									EXEC('
									CREATE PROCEDURE [dbo].[sp_UpdateStatusServerSchedule]
										@status bit, @site nvarchar(256)
									AS
									BEGIN
										UPDATE S_ServerScheduleStatus SET Status = @status, ModificationDate = GETDATE() WHERE Site = @site
									END
									');
								END";

                    this.RootContext.Context.RawSqlQuery(query);
                }
                catch (Exception ex)
                {
                    new IboxLog($"Create Store sp_UpdateStatusServerSchedule an unexpected error has occurred: {ex}", "AppLogs", ex);
                }
            }

            private void CreateStoreListenStatusServerShrinkLog()
            {
                try
                {
                    string query = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateStatusServerShrinkLog]') AND type in (N'P'))
									BEGIN
										EXEC('
										Create PROCEDURE [dbo].[sp_UpdateStatusServerShrinkLog]
										@status bit, @site nvarchar(256)
										AS
										BEGIN
											update S_ServerScheduleShrinkLogStates set Status = @status, ModificationDate = getdate() where Site = @site
										END
										');
									END";

                    this.RootContext.Context.RawSqlQuery(query);
                }
                catch (Exception ex)
                {
                    new IboxLog($"Create Store sp_UpdateStatusServerShrinkLog an unexpected error has occurred: {ex}", "AppLogs", ex);
                }
            }
        }

        // just do check environment or start, stop and restart services

        /// <summary>
        /// Check Environment
        /// </summary>
        /// <returns></returns>
        public object CheckEnvironment()
        {
            var environment = new
            {
                Database = new List<Environment>()
                {
                    new Environment() { Name = "Database engine", State = false },
                    new Environment() { Name = "IBox user admin config", State = false },
                },
                Service = new List<Environment>()
                {
                    new Environment(){ Name = "IBoxClient Service", State = true },
                    new Environment(){ Name = "Background Service", State = true },
                    new Environment(){ Name = "Rest Service", State = true }
                }
            };

            // Database engine
            var isServerConnected = this.RootContext.Context.Database.CanConnect();
            if (isServerConnected)
            {
                int index = environment.Database.FindIndex(env => env.Name == "Database engine");
                environment.Database[index].State = true;
            }

            // Admin User
            if (isServerConnected)
            {
                var isUserAdmin = this.RootContext.Context.U_Users.Any();
                if (isUserAdmin)
                {
                    int index = environment.Database.FindIndex(env => env.Name == "IBox user admin config");
                    environment.Database[index].State = true;
                }
            }

            return environment;
        }
    }

    public class BTenantEnvironment : BBaseBusiness<BTenantEnvironment>
    {
        public class BTenantEnvironmentBuilder : BBaseBusiness<BTenantEnvironmentBuilder>
        {
            private string? tenantName;
            private string? email;
            private string? phone;
            private string? address;
            private string? tenantAdmin = string.Empty;
            private string? tenantAdminPassword = string.Empty;
            //private string domainEmail = string.Empty;

            public string? TenantName { get => tenantName; set => tenantName = value; }
            public string? Email { get => email; set => email = value; }
            public string? Phone { get => phone; set => phone = value; }
            public string? Address { get => address; set => address = value; }
            public string? TenantAdmin { get => tenantAdmin; set => tenantAdmin = value; }
            public string? TenantAdminPassword { get => tenantAdminPassword; set => tenantAdminPassword = value; }
            //public string DomainEmail { get => domainEmail; set => domainEmail = value; }

            public void ConfigTenantDatabase(BTenantEnvironmentBuilder payLoad)
            {
                try
                {
                    CheckIsValidField(payLoad);
                    var tenantid = RegistTenantInRoot(payLoad);
                    CreateIBoxTenantAdmin(tenantid);
                    CreateTenantDatabase(tenantid);
                    CreateConnectionDefaultTenant(tenantid);
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"{ex.Message}", "AppLogs", ex);
                }
            }

            private string RegistTenantInRoot(BTenantEnvironmentBuilder payLoad)
            {
                var tenant = this.RootContext.Context.T_Tenants.FirstOrDefault(ptr => ptr.TenantName.Equals(tenantName));

                if (tenant != null)
                {
                    throw new IboxLog("Customer name was registed, please try again.", "AppLogs");
                }

                var userAdmin = this.RootContext.Context.U_Users.FirstOrDefault(user => user.UserName == payLoad.TenantAdmin);

                if (userAdmin != null)
                {
                    throw new IboxLog("Tenant account have been existed!", "AppLogs");
                }

                var tenantTemp = new T_Tenant()
                {
                    TenantName = tenantName,
                    Address = address,
                    Email = email,
                    Phone = phone,
                    MailBCC = string.Empty,
                    MailBody = string.Empty,
                    MailCC = string.Empty,
                    MailTitle = string.Empty,
                    MailTo = string.Empty,
                    Smtp_Host = string.Empty,
                    Smtp_Port = string.Empty,
                    Smtp_Username = string.Empty,
                    Smtp_Password = string.Empty,
                };
                tenantTemp.TenantCode = tenantTemp.FormatToCode(tenantName ?? "", "AppLogs");
                tenant.TryCreate(this.RootContext.Context, tenantTemp);
                return tenantTemp.Id;
            }

            private void CheckIsValidField(BTenantEnvironmentBuilder payLoad)
            {
                string patternPasswordAdmin = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$";
                string patternTenantAdmin = @"^(?=.{8,64}$)[A-Za-z0-9._-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
                //string patternTenantAdmin = @"^[a-zA-Z0-9.]{4,64}$";
                //string patternDomain = @"^@[a-z0-9.-]+\.[a-z]{2,}$";

                if (string.IsNullOrEmpty(payLoad.TenantName))
                {
                    throw new IboxLog("User Admin cannot be empty.", "AppLogs");
                }

                if (string.IsNullOrEmpty(payLoad.TenantAdmin))
                {
                    throw new IboxLog("User Admin cannot be empty.", "AppLogs");
                }

                if (!Regex.IsMatch(payLoad.TenantAdmin, patternTenantAdmin, RegexOptions.None, TimeSpan.FromMilliseconds(300)))
                {
                    //throw new IboxException("User Admin must be 4-64 characters long and can only contain letters, numbers, and periods.");
                    throw new IboxLog("User Admin must be in email format and 8 - 64 characters.", "AppLogs");
                }

                //if (string.IsNullOrEmpty(payLoad.DomainEmail))
                //{
                //    throw new IboxException("Domain email cannot be empty.");
                //}

                //if (!Regex.IsMatch(payLoad.DomainEmail, patternDomain, RegexOptions.None, TimeSpan.FromMilliseconds(300)))
                //{
                //    throw new IboxException("Domain email must be in valid format (e.g., @gmail.com).");
                //}

                if (string.IsNullOrEmpty(payLoad.TenantAdminPassword))
                {
                    throw new IboxLog("Password Admin cannot be empty.", "AppLogs");
                }

                if (!Regex.IsMatch(payLoad.TenantAdminPassword, patternPasswordAdmin, RegexOptions.None, TimeSpan.FromMilliseconds(300)))
                {
                    throw new IboxLog("Password Admin must 8 - 32 characters, contain at least one uppercase letter, one lowercase letter, one number and one special character.", "AppLogs");
                }
            }

            private AdminState CreateIBoxTenantAdmin(string tenantID)
            {
                try
                {
                    var isDatabaseConnected = this.RootContext.Context.Database.CanConnect();
                    if (isDatabaseConnected)
                    {
                        //var userAdmin = this.RootContext.Context.U_Users.FirstOrDefault(user => user.UserName == (this.tenantAdmin + this.domainEmail));
                        var userAdmin = this.RootContext.Context.U_Users.FirstOrDefault(user => user.UserName == this.tenantAdmin);
                        if (userAdmin == null)
                        {

                            userAdmin.TryCreate(this.RootContext.Context, new U_User()
                            {
                                //UserName = this.tenantAdmin + this.domainEmail,
                                UserName = this.tenantAdmin,
                                Password = this.Encryption.SHAEncode(this.tenantAdminPassword),
                                RoleID = "TENANT",
                                TenantID = tenantID,
                                SKey = string.Empty
                            });

                        }
                        else
                        {
                            throw new IboxLog("Tenant account have been existed!", tenantID);
                        }
                    }

                    var userAdminExist = this.RootContext.Context.U_Users.FirstOrDefault(user => user.UserName == this.tenantAdmin);
                    if (userAdminExist == null)
                    {
                        return AdminState.CreateFailed;
                    }

                    return AdminState.CreateSuccess;
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"{ex.Message}", tenantID, ex);
                }
            }

            private DatabaseState CreateTenantDatabase(string tenantID)
            {
                try
                {

                    var tenantContext = this.tenantContext.GetTenantContext(tenantID);

                    var isDatabaseConnected = tenantContext.Context.Database.CanConnect();
                    if (!isDatabaseConnected)
                    {
                        tenantContext.Context.Database.EnsureCreated();
                        tenantContext.Context.EnsureViewsCreated();
                    }
                    else
                    {
                        return DatabaseState.ReadyToUse;
                    }

                    Thread.Sleep(3000);

                    isDatabaseConnected = tenantContext.Context.Database.CanConnect();

                    if (!isDatabaseConnected)
                    {
                        return DatabaseState.CreateFailed;
                    }

                    return DatabaseState.CreateSuccess;
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"{ex.Message}", tenantID, ex);
                }
            }

            private void CreateConnectionDefaultTenant(string tenantId)
            {
                try
                {
                    var tContext = new TenantContext(this.Configuration, this.Encryption, new RootContext(this.Configuration, this.Encryption));
                    var tenantContext = tContext.GetTenantContext(tenantId).Context;

                    var isDatabaseConnected = tenantContext.Database.CanConnect();

                    if (isDatabaseConnected)
                    {
                        var connection = tenantContext.Database.GetDbConnection();
                        var connectionString = connection.ConnectionString;
                        var builder = new SqlConnectionStringBuilder(connectionString);

                        EntityAction.TryCreate<DatabaseConnection>(null, tenantContext, new DatabaseConnection()
                        {
                            Address = builder.DataSource,
                            Catalog = builder.InitialCatalog,
                            Username = builder.UserID,
                            Password = builder.Password,
                            Port = "",
                            SQLType = 0,
                            Options = $@"[{{""id"":""{Guid.NewGuid()}"",""key"":""MultiSubnetFailover"",""value"":""{builder.MultiSubnetFailover}""}},
                                          {{""id"":""{Guid.NewGuid()}"",""key"":""TrustServerCertificate"",""value"":""{builder.TrustServerCertificate}""}},
                                          {{""id"":""{Guid.NewGuid()}"",""key"":""MultipleActiveResultSets"",""value"":""{builder.MultipleActiveResultSets}""}},
                                          {{""id"":""{Guid.NewGuid()}"",""key"":""Pooling"",""value"":""{builder.Pooling}""}},
                                          {{""id"":""{Guid.NewGuid()}"",""key"":""Min Pool Size"",""value"":""{builder.MinPoolSize}""}},
                                          {{""id"":""{Guid.NewGuid()}"",""key"":""Max Pool Size"",""value"":""{builder.MaxPoolSize}""}}]"
                        });

                        tenantContext.Dispose();
                    }
                    else
                    {
                        new IboxLog($"CreateConnectionDefaultTenant fail: can not connect database.", "AppLogs");
                    }
                }
                catch (Exception ex)
                {
                    new IboxLog($"CreateConnectionDefaultTenant Error: {ex}", "AppLogs");
                }
            }
        }
    }

    public enum DatabaseState
    {
        CreateSuccess,
        CreateFailed,
        ReadyToUse
    }

    public enum AdminState
    {
        CreateSuccess,
        CreateFailed
    }
}