using IBox.Common.Objects;
using Microsoft.AspNetCore.Http;

namespace IBox.Common.FolderLog
{
    public interface IRestLog
    {
        public void WriteFileLogWF(string serviceName, object obj);
        public void WriteFileLogAPI(string serviceName, object obj);
        public void WriteFileLogSQL(string serviceName, object obj);
        public List<LogInfo> ListLog(string Location);
        public Task StreamDataLogCache(HttpResponse Response, HttpContext httpContext);
        public Task AddLogCacheAsync(string type, string log);
    }
}