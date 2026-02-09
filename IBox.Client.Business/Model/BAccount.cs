using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using System.Text.RegularExpressions;

namespace IBox.Client.Business.Model
{
    public class BAccount : BBaseBusiness<BAccount>
    {
        public class Signin : BBaseBusiness<Signin>
        {
            private string? _userName;
            private string? _password;
            private string? _tokenRefresh;

            public string? UserName { get => _userName; set => _userName = value; }
            public string? Password { get => _password; set => _password = value; }
            public string? TokenRefresh { get => _tokenRefresh; set => _tokenRefresh = value; }

            public string TokenGeneration()
            {
                U_User user = new U_User();
                if (string.IsNullOrEmpty(TokenRefresh))
                {

                    user = this.RootContext.Context.U_Users.FirstOrDefault(ptr => ptr.UserName == this._userName);


                    if (user == null)
                    {
                        throw new IboxLog("Account or password was incorrect.", "AppLogs");
                    }

                    if (string.IsNullOrEmpty(this._password))
                    {
                        throw new IboxLog("Password is empty!", "AppLogs");
                    }

                    if (user.IsDelete)
                    {
                        throw new IboxLog("Account has been locked.", "AppLogs");
                    }

                    var roleUser = this.RootContext.Context.U_Roles.FirstOrDefault(ptr => ptr.Id == user.RoleID && !ptr.IsDelete);

                    var permissionsKey = roleUser?.PermissionsKey ?? "[]";

                    var hashPassword = this.Encryption.SHAEncode(this._password);
                    if (hashPassword == user.Password)
                    {
                        this.RestAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                        {
                            Id = user.TenantID,
                            MailTitle = "[Warning] Hệ thống đang bị truy cập",
                            MailBody = $"{user.UserName}",
                            typeWarning = TypeWarning.LoginTenant
                        });

                        return this.Encryption.JWT(user.TenantID ?? string.Empty, user.UserName ?? string.Empty,
                            (Skey) =>
                            {
                                user.TryUpdate(this.RootContext.Context, new U_User()
                                {
                                    Id = user.Id,
                                    SKey = Skey,
                                    SKey_Expires = DateTime.Now.AddMinutes(this.Configuration.Config.Value.JWT.TotalMinuteAlive - 1),
                                });
                            }
                            , new KeyValuePair<string, string>("Tenant", user.TenantID ?? string.Empty)
                            , new KeyValuePair<string, string>("Role", user.RoleID ?? string.Empty)
                            , new KeyValuePair<string, string>("UserId", user.Id ?? string.Empty)
                            , new KeyValuePair<string, string>("PermissionsKey", permissionsKey)
                            );
                    }
                    else
                    {
                        throw new IboxLog("Account or password was incorrect.", "AppLogs");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(TokenRefresh))
                    {
                        throw new IboxLog("TokenRefresh is required", "AppLogs");
                    }


                    user = this.RootContext.Context.U_Users.FirstOrDefault(ptr => ptr.SKey == TokenRefresh);


                    if (user == null)
                    {
                        throw new IboxLog("Account or password was incorrect.", "AppLogs");
                    }

                    if (user.SKey_Expires < DateTime.Now)
                    {
                        throw new IboxLog("Session was ended.", "AppLogs");
                    }

                    this.RestAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                    {
                        Id = user.TenantID,
                        MailTitle = "[Warning] Hệ thống đang bị truy cập",
                        MailBody = $"{user.UserName}",
                        typeWarning = TypeWarning.LoginTenant
                    });

                    return this.Encryption.JWT(user.TenantID ?? string.Empty, user.UserName ?? string.Empty,
                    (Skey) =>
                    {
                        user.TryUpdate(this.RootContext.Context, new U_User()
                        {
                            Id = user.Id,
                            SKey = Skey,
                            SKey_Expires = DateTime.Now.AddMinutes(this.Configuration.Config.Value.JWT.TotalMinuteAlive - 1),
                        });
                    }
                        , new KeyValuePair<string, string>("Tenant", user.TenantID ?? string.Empty)
                        , new KeyValuePair<string, string>("Role", user.RoleID ?? string.Empty)
                        , new KeyValuePair<string, string>("UserId", user.Id ?? string.Empty)
                        );
                }
            }

            public void TokenDestroy()
            {
                var user = this.RootContext.Context.U_Users.FirstOrDefault(ptr => ptr.UserName == this._userName);
                if (user == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(user.SKey))
                {
                    return;
                }

                user.TryUpdate(this.RootContext.Context, new U_User()
                {
                    Id = user.Id,
                    SKey = ""
                });
            }
        }

        public class Password : BBaseBusiness<Password>
        {
            private string? _currentPassword;
            private string? _newPassword;

            public string? CurrentPassword { get => _currentPassword; set => _currentPassword = value; }
            public string? NewPassword { get => _newPassword; set => _newPassword = value; }

            public bool ChangePassword(string userName)
            {
                string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$";

                if (string.IsNullOrEmpty(userName))
                {
                    throw new IboxLog("Account or password was incorrect", "AppLogs");
                }

                if (string.IsNullOrEmpty(this._currentPassword))
                {
                    throw new IboxLog("Current Password is empty!.", "AppLogs");
                }

                if (string.IsNullOrEmpty(this._newPassword))
                {
                    throw new IboxLog("New password is empty!.", "AppLogs");
                }

                if (!Regex.IsMatch(this._newPassword, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(300)))
                {
                    throw new IboxLog("New password must 8 - 32 characters, contain at least one uppercase letter, one lowercase letter, one number and one special character.", "AppLogs");
                }

                if (this._currentPassword == this._newPassword)
                {
                    throw new IboxLog("The new password must be different from the old password.", "AppLogs");
                }

                var user = this.RootContext.Context.U_Users.FirstOrDefault(ptr => ptr.UserName == userName);
                if (user == null)
                {
                    throw new IboxLog("Account or password was incorrect", "AppLogs");
                }

                var hashPassword = this.Encryption.SHAEncode(this._currentPassword);
                if (hashPassword == user.Password)
                {
                    var hashNewPassword = this.Encryption.SHAEncode(this._newPassword);
                    user.Password = hashNewPassword;
                    this.RootContext.Context.SaveChanges();
                    return true;
                }
                else
                {
                    throw new IboxLog("Account or password was incorrect", "AppLogs");
                }
            }
        }

        public class AccountDetail : BBaseBusiness<AccountDetail>
        {
            private string? _userID;
            private string? _userName;
            private string? _avatar;

            public string? UserID { get => _userID; set => _userID = value; }
            public string? FullName { get => _userName; set => _userName = value; }
            public string? Avatar { get => _avatar; set => _avatar = value; }

            public AccountDetail? Get()
            {
                if (string.IsNullOrEmpty(this._userID))
                {
                    throw new IboxLog("user id null or empty", "AppLogs");
                }

                AccountDetail? detail = this.RootContext
                    .Context
                    .U_User_Details
                    .Select(ptr => new AccountDetail()
                    {
                        UserID = ptr.UserID,
                        FullName = ptr.FullName,
                        Avatar = ptr.Avatar,
                    })
                    .FirstOrDefault(ptr => ptr.UserID == this._userID);

                if (detail == null)
                {
                    return null;
                }

                return detail;
            }
        }

        public class DeleteAccount : BBaseBusiness<DeleteAccount>
        {
            private string? tenantId;
            private string? rootPassword;
            public string? TenantId { get => tenantId; set => tenantId = value; }
            public string? RootPassword { get => rootPassword; set => rootPassword = value; }

            public DeleteAccount()
            { }

            /// <summary>
            /// Kiểm tra password root trước khi xóa tenant
            /// </summary>
            /// <param name="deleteAccount"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public string CheckIsValidRootPassword(DeleteAccount deleteAccount)
            {
                if (string.IsNullOrEmpty(deleteAccount.RootPassword))
                {
                    throw new IboxLog("Root password confirm can not null or emty.", "AppLogs");
                }

                if (string.IsNullOrEmpty(deleteAccount.TenantId))
                {
                    throw new IboxLog("01: Invalid password.", "AppLogs");
                }

                var userRoot = this.RootContext.Context.U_Users.Where(user => user.RoleID == "ROOT" && user.TenantID == "ROOT").FirstOrDefault();

                if (userRoot == null)
                {
                    throw new IboxLog("02: Invalid password.", "AppLogs");
                }

                var hashPassword = this.Encryption.SHAEncode(deleteAccount.RootPassword);

                if (userRoot.Password != hashPassword)
                {
                    EntityAction.TryCreate<U_User_Action_History>(null, this.RootContext.Context, new U_User_Action_History()
                    {
                        UserID = userRoot.Id,
                        UserAction = UserAction.Delete,
                        ActionName = nameof(ActionName.ConfigTenant),
                        ActionStatus = ActionStatus.Fail
                    });

                    Thread.Sleep(500);

                    var actionDeleteFail = this.RootContext.Context.U_User_Action_Historys.Where(ptr => ptr.UserID == userRoot.Id && ptr.ActionName == nameof(ActionName.ConfigTenant)).OrderByDescending(ptr => ptr.CreatedDate).Take(5).ToList();

                    if (!actionDeleteFail.Any(ptr => ptr.ActionStatus == ActionStatus.Success) &&
                        !actionDeleteFail.Any(ptr => ptr.UserAction == UserAction.Delete &&
                        ptr.ActionStatus == ActionStatus.Success))
                    {
#pragma warning disable CS8629 // Nullable value type may be null.
                        userRoot.TryUpdate(this.RootContext.Context, new U_User()
                        {
                            Id = userRoot.Id,
                            SKey = string.Empty,
                            SKey_Expires = userRoot.SKey_Expires.Value.AddMinutes(-15)
                        });
#pragma warning restore CS8629 // Nullable value type may be null.
                    }

                    throw new IboxLog("03: Invalid password.", "AppLogs");
                }

                EntityAction.TryCreate<U_User_Action_History>(null, this.RootContext.Context, new U_User_Action_History()
                {
                    UserID = userRoot.Id,
                    UserAction = UserAction.Delete,
                    ActionName = nameof(ActionName.ConfigTenant),
                    ActionStatus = ActionStatus.Success
                });


                DeleteTenant(deleteAccount.tenantId);


                return "Success";
            }

            /// <summary>
            /// Xóa tenant
            /// </summary>
            /// <param name="tenantId"></param>
            /// <exception cref="Exception"></exception>
            private void DeleteTenant(string tenantId)
            {
                var userAdmin = this.RootContext.Context.U_Users.FirstOrDefault(ptr => ptr.TenantID == tenantId);

                if (userAdmin != null)
                {
                    string userid = userAdmin.Id;

                    userAdmin.TryUpdate(this.RootContext.Context, new U_User()
                    {
                        Id = userid,
                        IsDelete = true,
                    });
                    RestAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                    {
                        Id = tenantId,
                        MailTitle = "[Warning] Tenant bị xóa",
                        MailBody = $"{userAdmin.UserName}",
                        typeWarning = TypeWarning.TenantDelete
                    });
                }
                else
                {
                    throw new IboxLog("04: Invalid password.", "AppLogs");
                }

                var tenant = this.RootContext.Context.T_Tenants.FirstOrDefault(ptr => ptr.Id == tenantId);

                if (tenant != null)
                {
                    tenant.TryUpdate(this.RootContext.Context, new T_Tenant()
                    {
                        Id = tenantId,
                        IsDelete = true,
                    });
                }
                else
                {
                    throw new IboxLog("05: Invalid password.", "AppLogs");
                }
            }
        }

        private string? _userid;

        public AccountDetail Detail
        {
            get
            {
                if (!string.IsNullOrEmpty(_userid))
                {
                    var userDetail = this.RootContext
                        .Context
                        .U_User_Details
                        .FirstOrDefault(ptr => ptr.UserID == this._userid);

                    if (userDetail != null)
                    {
                        return new AccountDetail()
                        {
                            UserID = userDetail.UserID,
                            FullName = userDetail.FullName,
                            Avatar = userDetail.Avatar,
                        };
                    }
                }

                return new AccountDetail();
            }
        }

        public class CreateAccount : BBaseBusiness<CreateAccount>
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string RoleId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;

            public CreateAccount()
            { }

            public bool CreateAccountTenant(string tenantId)
            {
                string patternPass = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$";

                if (string.IsNullOrEmpty(tenantId))
                {
                    throw new IboxLog("TenantId can not be null or empty.", "AppLogs");
                }

                if (string.IsNullOrEmpty(this.UserName))
                {
                    throw new IboxLog("UserName can not be null or empty.", "AppLogs");
                }

                if (!Regex.IsMatch(this.Password, patternPass, RegexOptions.None, TimeSpan.FromMilliseconds(300)))
                {
                    throw new IboxLog("New password must 8 - 32 characters, contain at least one uppercase letter, one lowercase letter, one number and one special character.", "AppLogs");
                }

                var user = this.RootContext.Context.U_Users.FirstOrDefault(ptr => ptr.UserName == UserName && ptr.TenantID == tenantId);

                if (user != null)
                {
                    throw new IboxLog("UserName already exists.", "AppLogs");
                }   

                var userId = Guid.NewGuid().ToString();

                EntityAction.TryCreate(null, this.RootContext.Context, new U_User()
                {
                    Id = userId,
                    UserName = UserName,
                    Password = this.Encryption.SHAEncode(this.Password),
                    RoleID = RoleId,
                    TenantID = tenantId
                });

                return true;
            }
        }

        public string? Userid { get => _userid; set => _userid = value; }
    }
}