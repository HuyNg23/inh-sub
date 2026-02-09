using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers.ConfigRoot
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class ConfigRootController : ActionController<C_ConfigRoot, RootContext>
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public ConfigRootController(IBContext<RootContext> roottContext, Common.Objects.IConfiguration configuration, IEncryption encryption) : base(roottContext)
        {
            _configuration = configuration;
            _encryption = encryption;
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var configRoot = CheckValid(req);

                if (string.IsNullOrEmpty(configRoot.TypeConfig))
                {
                    throw new IboxLog("Type is not null or emty.", "AppLogs");
                }

                var rootContext = new RootContext(this._configuration, this._encryption);
                var configData = rootContext.Context.C_ConfigRoot.FirstOrDefault(x => x.TypeConfig == configRoot.TypeConfig && !x.IsDelete);
                rootContext.Context.Dispose();

                if (configData != null)
                {
                    throw new IboxLog("Config Name already exist", "AppLogs");
                }

                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var configRoot = CheckValid(req);

                if (string.IsNullOrEmpty(configRoot.TypeConfig))
                {
                    throw new IboxLog("Type is not null or emty.", "AppLogs");
                }

                var rootContext = new RootContext(this._configuration, this._encryption);
                var configData = rootContext.Context.C_ConfigRoot.FirstOrDefault(x => x.TypeConfig == configRoot.TypeConfig && !x.IsDelete);
                rootContext.Context.Dispose();

                if (configData == null)
                {
                    throw new IboxLog("Config Name does not exist", "AppLogs");
                }

                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("GetTypeConfig/{type}")]
        public ResponseForm<List<dynamic>> GetTypeConfig(RequestForm<dynamic> req, int type)
        {
            try
            {
                return new ResponseForm<List<dynamic>>(() =>
                {
                    CheckValid(req);
                    var rootContext = new RootContext(this._configuration, this._encryption);
                    var responeConfigRoot = rootContext.Context.C_ConfigRoot.Where(x => x.TypeConfig == type.ToString() && !x.IsDelete).ToList<dynamic>();
                    rootContext.Context.Dispose();

                    return responeConfigRoot;
                });
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<dynamic>>(() => throw ex);
            }
        }
    }
}