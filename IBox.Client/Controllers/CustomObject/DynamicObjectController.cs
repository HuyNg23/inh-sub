using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Security;
using IBox.Workflow.Execution;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Tenant.CustomObject
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class DynamicObjectController : ControllerBase
    {
        private readonly IModelControl _modelControl;
        private readonly IBaseObjectBuilder _objectBuilder;
        private readonly IBContext<TenantContext> _tenantContext;

        public DynamicObjectController(IModelControl modelControl, IBaseObjectBuilder objectBuilder, IBContext<TenantContext> tenantContext)
        {
            _modelControl = modelControl;
            _tenantContext = tenantContext;
            _objectBuilder = objectBuilder;
        }

        [HttpPost("BuildObject")]
        [IBoxActionPermission("integration-config-model-manage")]
        public ResponseForm<object> BuildObject(string objectId)
        {
            return new ResponseForm<object>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                _modelControl.SetTenantContext(_tenantContext, tenantID);
                _modelControl.BuildModel(objectId, tenantID, _tenantContext);
            });
        }

        [HttpPost("GetJsonDump")]
        [IBoxActionPermission("integration-config-model-manage", "integration-config-model-view")]
        public ResponseForm<object> GetJsonDump(RequestForm<PayLoad> req)
        {
            return new ResponseForm<object>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                _modelControl.SetTenantContext(_tenantContext, tenantID);
                _modelControl.BuildModel(req.Body.ObjectId, tenantID, _tenantContext);
                return _modelControl.BindingDataDump(req.Body.ObjectId, tenantID);
            });
        }

        [HttpPost("SetValuePublic")]
        [IBoxActionPermission("integration-config-model-manage")]
        public ResponseForm<object> SetValue(RequestForm<PayLoad> payLoad)
        {
            return new ResponseForm<object>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                _modelControl.SetTenantContext(_tenantContext, tenantID);
                return _modelControl.BindingData(payLoad.Body.ObjectId, payLoad.Body.Data, tenantID);
            });
        }

        [HttpPost("GetObjectSchemaByID")]
        [IBoxActionPermission("integration-config-model-manage")]
        public ResponseForm<object> GetObjectSchemaByID(RequestForm<PayLoad> payLoad)
        {
            return new ResponseForm<object>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                _modelControl.SetTenantContext(_tenantContext, tenantID);
                return _objectBuilder.GetObjectzInBD(payLoad.Body.ObjectId);
            });
        }
    }

    public class PayLoad
    {
        private string objectId = string.Empty;
        private dynamic data = string.Empty;

        public string ObjectId { get => objectId; set => objectId = value; }
        public dynamic Data { get => data; set => data = value; }
    }
}