using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using static IBox.Client.Business.Model.BAccount;

namespace IBox.Client.Controllers.XUser
{
    [Route("api/[controller]")]
    [ApiController]
    public class XAccountController : ActionController<U_User, RootContext>
    {
        private readonly IServiceProvider _serviceProvider;

        public XAccountController(IBContext<RootContext> mainDB, IServiceProvider serviceProvider, IEncryption encryption) : base(mainDB, encryption)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpPost("Signin")]
        public ResponseForm<string> Signin(RequestForm<BAccount.Signin> req)
        {
            return new ResponseForm<string>(() =>
            {
                return req.Body.Init(_serviceProvider).TokenGeneration();
            });
        }

        [HttpPost("Signout")]
        public ResponseForm<string> Signout(RequestForm<BAccount.Signin> req)
        {
            return new ResponseForm<string>(() =>
            {
                req.Body.Init(_serviceProvider).TokenDestroy();
                return "Ok";
            });
        }

        [HttpPost("ChangePassword")]
        [IBoxAuthorization]
        public ResponseForm<string> ChangePassword(RequestForm<BAccount.Password> req)
        {
            return new ResponseForm<string>(() =>
            {
                var user = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.User.ToString()).Value.ToString() ?? string.Empty;
                return req.Body.Init(_serviceProvider).ChangePassword(user).ToString() ?? "false";
            });
        }

        [HttpPost("RefreshToken")]
        [IBoxAuthorization]
        public ResponseForm<string> RefreshToken(RequestForm<BAccount.Signin> req)
        {
            return new ResponseForm<string>(() =>
            {
                return req.Body.Init(_serviceProvider).TokenGeneration();
            });
        }

        [HttpPost("AccountDetail")]
        [IBoxAuthorization]
        public ResponseForm<BAccount.AccountDetail> AccountDetail(RequestForm<BAccount> req)
        {
            return new ResponseForm<BAccount.AccountDetail>(() =>
            {
                return req.Body.Init(_serviceProvider).Detail;
            });
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", "AppLogs");
            });
        }

        [HttpPost("CreateUser")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("system-config-user-manage")]
        public ResponseForm<dynamic> Create(RequestForm<CreateAccount> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return req.Body.Init(_serviceProvider).CreateAccountTenant(tenantId);
            });
        }

        [HttpPost("GetAll/{pageNumber}")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("system-config-user-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            try
            {
                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                return new ResponseForm<List<dynamic>>(() =>
                {
                    if (pageNumber == -1)
                    {
                        var queryResultPage = (from user in base.IBContext.Context.U_Users
                                               join role in base.IBContext.Context.U_Roles
                                               on user.RoleID equals role.Id into roleGroup
                                               from role in roleGroup.DefaultIfEmpty()

                                               where user.TenantID == tenantId && user.RoleID != "TENANT" && !user.IsDelete
                                               orderby user.CreatedDate descending
                                               select new
                                               {
                                                   Id = user.Id,
                                                   UserName = user.UserName,
                                                   CreatedDate = user.CreatedDate,
                                                   ModificationDate = user.ModificationDate,
                                                   RoleId = user.RoleID ?? string.Empty,
                                                   RoleName = role.Name ?? string.Empty
                                               }).ToList<dynamic>();

                        return queryResultPage;
                    }
                    else
                    {
                        int numberOfObjectsPerPage = 30;
                        var queryResultPage = (from user in base.IBContext.Context.U_Users
                                               join role in base.IBContext.Context.U_Roles
                                               on user.RoleID equals role.Id into roleGroup
                                               from role in roleGroup.DefaultIfEmpty()

                                               where user.TenantID == tenantId && user.RoleID != "TENANT" && !user.IsDelete
                                               orderby user.CreatedDate descending
                                               select new
                                               {
                                                   Id = user.Id,
                                                   UserName = user.UserName,
                                                   CreatedDate = user.CreatedDate,
                                                   ModificationDate = user.ModificationDate,
                                                   RoleId = user.RoleID ?? string.Empty,
                                                   RoleName = role.Name ?? string.Empty
                                               })
                                            .Skip(numberOfObjectsPerPage * (pageNumber - 1))
                                            .Take(numberOfObjectsPerPage).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();

                        return queryResultPage;
                    }
                });
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<dynamic>>(() => throw ex);
            }
        }
    }
}