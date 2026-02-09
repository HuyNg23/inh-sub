using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class SecurityController : ControllerBase
    {
        private readonly IEncryption _encryption;

        public SecurityController(IEncryption encryption)
        {
            _encryption = encryption;
        }

        [HttpPost("EnCodeIBox")]
        public ResponseForm<ResponseSecurity> EnCodeIBoxAsync(RequestForm<DataSecurity> request)
        {
            try
            {
                return new ResponseForm<ResponseSecurity>(() =>
                {
                    return new ResponseSecurity()
                    {
                        data = _encryption.Encrypt(request.Body.dataCode)
                    };
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Error EnCodeIBox: {ex.Message}");
                throw;
            }
        }

        [HttpPost("DeCodeIBox")]
        public ResponseForm<ResponseSecurity> DeCodeIBoxAsync(RequestForm<DataSecurity> request)
        {
            try
            {
                return new ResponseForm<ResponseSecurity>(() =>
                {
                    return new ResponseSecurity()
                    {
                        data = _encryption.Decrypt(request.Body.dataCode)
                    };
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Error EnCodeIBox: {ex.Message}");
                throw;
            }
        }

        public class DataSecurity
        {
            private string dataCode1 = string.Empty;
            private string key1 = "B@sebs1234";

            public string dataCode { get => dataCode1; set => dataCode1 = value; }
            public string key { get => key1; set => key1 = value; }
        }

        public class ResponseSecurity
        {
            private string data1 = string.Empty;
            public string data { get => data1; set => data1 = value; }
        }
    }
}