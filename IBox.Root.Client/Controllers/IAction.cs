using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IBox.Root.Client.Controllers
{
    public interface IAction
    {
        public ResponseForm<dynamic> Create(RequestForm<dynamic> req);

        public ResponseForm<dynamic> Update(RequestForm<dynamic> req);

        public ResponseForm<dynamic> Delete(RequestForm<dynamic> req);

        public ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber);

        public ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id);
    }

    public abstract class ActionController<T, TContext> : ControllerBase, IAction where T : BaseTable where TContext : AibContext
    {
        private readonly IBContext<TContext> _context;
        private IBContext<TContext> _iBContext;

        protected IBContext<TContext> IBContext
        {
            get
            {
                if (_iBContext == null)
                {
                    var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                    _iBContext = _context.GetTenantContext(tenantID);
                    return _iBContext;
                }
                else
                {
                    return _iBContext;
                }
            }
        }

        protected T CheckValid(RequestForm<dynamic> req)
        {
            try
            {
                var str_json = req.Body?.ToString();
                if (str_json == null)
                {
                    throw new IboxLog("Can not convert object to json", "AppLogs");
                }

                T? record = JsonConvert.DeserializeObject<T>(str_json);
                if (record == null)
                {
                    throw new IboxLog(string.Format("Can not convert to [{0}] model", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }

                return record;
            }
            catch
            {
                var templateObject = Activator.CreateInstance(typeof(T));
                if (templateObject == null)
                {
                    throw new IboxLog("Can not create Instance in check valid object parameter", "AppLogs");
                }

                return (T)templateObject;
            }
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected ActionController(IBContext<TContext> Context)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            if (Context == null)
            {
                throw new IboxLog("Can not create an intance object database", "AppLogs");
            }
            _context = Context;
        }

        [HttpPost("Create")]
        public virtual ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                return CheckValid(req).Create<T, TContext>(this.IBContext);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("Update")]
        public virtual ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                return CheckValid(req).Update<T, TContext>(this.IBContext);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("Delete")]
        public virtual ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            try
            {
                return CheckValid(req).Delete<T, TContext>(this.IBContext);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("RollbackDelete")]
        public virtual ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            try
            {
                return CheckValid(req).RollbackDelete<T, TContext>(this.IBContext);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("GetAll/{pageNumber}")]
        public virtual ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            var startTime = DateTime.Now.ToUniversalTime();
            try
            {
                return CheckValid(req).GetAll<T, TContext>(this.IBContext, pageNumber);
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<dynamic>>(() => throw ex);
            }
        }

        [HttpPost("GetDetail/{id}")]
        public virtual ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            try
            {
                return CheckValid(req).GetDetail<T, TContext>(this.IBContext, id);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("GetAllDeleted/{pageNumber}")]
        public virtual ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            var startTime = DateTime.Now.ToUniversalTime();
            try
            {
                return CheckValid(req).GetAllDeleted<T, TContext>(this.IBContext, pageNumber);
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<dynamic>>(() => throw ex);
            }
        }
    }
}