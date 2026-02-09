using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers.ServerSite
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootTenantAuthorization]
    public class ServerSiteController : ControllerBase
    {
        [HttpPost("GetAll/{pageNumber}")]
        public ResponseForm<List<S_ServerSite>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return new ResponseForm<List<S_ServerSite>>(() =>
            {
                var serverSites = IBGlobalConfig.Services
                    .Select(ptr =>  new S_ServerSite { Name = ptr.Site, Site = ptr.Site })
                    .DistinctBy(s => s.Site)
                    .ToList();
                return serverSites;
            });
        }
        public class S_ServerSite
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string? Name { get; set; } = "";
            public string? Site { get; set; } = "";
        }
    }
}
