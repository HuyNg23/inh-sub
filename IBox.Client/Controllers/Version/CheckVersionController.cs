using IBox.Common.Objects;
using IBox.Common.Version;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Version
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckVersionController : ControllerBase
    {
        private readonly ICheckVersion _checkVersion;

        public CheckVersionController(ICheckVersion checkVersion)
        {
            _checkVersion = checkVersion;
        }

        [HttpGet]
        [Route("ShowVersion")]
        public ResponseForm<CheckVersionIBoxModel> ShowVersion()
        {
            try
            {
                return new ResponseForm<CheckVersionIBoxModel>(() =>
                {
                    return _checkVersion.ShowVersion($"{Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "version", "CHANGELOG.md")}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}