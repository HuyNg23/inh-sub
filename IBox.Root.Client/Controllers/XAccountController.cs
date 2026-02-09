using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using static IBox.Client.Business.Model.BAccount;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class XAccountController : ActionController<U_User, RootContext>
    {
        private readonly IServiceProvider _serviceProvider;

        public XAccountController(IBContext<RootContext> mainDB, IServiceProvider serviceProvider) : base(mainDB)
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
        [IBoxRootAuthorization]
        public ResponseForm<string> ChangePassword(RequestForm<BAccount.Password> req)
        {
            var user = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.User.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<string>(() => req.Body.Init(_serviceProvider).ChangePassword(user).ToString() ?? "false");
        }

        [HttpPost("AccountDetail")]
        [IBoxRootAuthorization]
        public ResponseForm<BAccount.AccountDetail> AccountDetail(RequestForm<BAccount> req)
        {
            return new ResponseForm<BAccount.AccountDetail>(() =>
            {
                return req.Body.Init(_serviceProvider).Detail;
            });
        }

        [HttpPost("RefreshToken")]
        [IBoxRootAuthorization]
        public ResponseForm<string> RefreshToken(RequestForm<BAccount.Signin> req)
        {
            return new ResponseForm<string>(() =>
            {
                return req.Body.Init(_serviceProvider).TokenGeneration();
            });
        }

        [HttpPost("CreateUser")]
        [IBoxAuthorization]
        [IBoxPermissions]
        public ResponseForm<dynamic> Create(RequestForm<CreateAccount> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return req.Body.Init(_serviceProvider).CreateAccountTenant(tenantId);
            });
        }
    }
}