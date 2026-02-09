using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using System.Runtime.Serialization;

namespace IBox.Common.Objects
{
    [Serializable]
    public class IboxLog : Exception
    {
        public IboxLog()
        {
        }

        public IboxLog(string? message, string tenantId, string Type = "Error") : base(message)
        {
            if (Type == "Error")
            {
                using (LogContext.PushProperty("TenantId", tenantId))
                {
                    Log.Error(message ?? "");
                }
            }
            else
            {
                using (LogContext.PushProperty("TenantId", tenantId))
                {
                    Log.Information(message ?? "");
                }
            }
        }

        public IboxLog(string? message, string tenantId, Exception? innerException) : base(message, innerException)
        {
            using (LogContext.PushProperty("TenantId", tenantId))
            {
                Log.Error(JsonConvert.SerializeObject(innerException));
            }
        }

        protected IboxLog(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}