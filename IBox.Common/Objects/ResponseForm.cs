using Serilog;

namespace IBox.Common.Objects
{
    public class ResponseForm<TOut> where TOut : class
    {
        private string message = string.Empty;
        private string code = string.Empty;
        private readonly double excutionTime = 0;
        private TOut? data;
        public ResponseForm()
        { 
        
        }
        public ResponseForm(Func<TOut> func)
        {
            var startTime = DateTime.Now.ToUniversalTime();
            try
            {
                if (func != null)
                {
                    this.data = func();
                    var endTime = DateTime.Now.ToUniversalTime();
                    this.excutionTime = (endTime - startTime).TotalMilliseconds;
                    this.message = "";
                    this.code = "0";
                }
            }
            catch (Exception ex)
            {
                new IboxLog(ex.Message,"AppLogs", ex);
                var endTime = DateTime.Now.ToUniversalTime();
                this.excutionTime = (endTime - startTime).TotalMilliseconds;
                this.message = ex.Message;
                this.code = "1";
            }
        }

        public ResponseForm(Action action)
        {
            var startTime = DateTime.Now.ToUniversalTime();
            try
            {
                if (action != null)
                {
                    action();
                    var endTime = DateTime.Now.ToUniversalTime();
                    this.excutionTime = (endTime - startTime).TotalMilliseconds;
                    this.message = "";
                    this.code = "0";
                }
            }
            catch (Exception ex)
            {
                new IboxLog(ex.Message, "AppLogs", ex);
                var endTime = DateTime.Now.ToUniversalTime();
                this.excutionTime = (endTime - startTime).TotalMilliseconds;
                this.message = ex.Message;
                this.code = "1";
            }
        }

        public string Message { get => message; set => message = value; }
        public string Code { get => code; set => code = value; }
        public TOut? Data { get => data; set => data = value; }
        public string ExcutionTime { get => string.Format("{0} ms", excutionTime); }
    }
}