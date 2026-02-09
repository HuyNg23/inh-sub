using IBox.Common.Security;
using IBox.DLEx.Execution;
using IBox.Workflow.Execution;
using System.Net.WebSockets;
using System.Text;

namespace IBox.Schedule.WebSocket
{
    public class ConnectWebSocket : IConnectWebSocket
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IExecuteWF _executeWF;
        private readonly IWorkflowControl _workflowControl;

        public ConnectWebSocket(Common.Objects.IConfiguration configuration, IEncryption encryption, IExecuteWF executeWF, IWorkflowControl workflowControl)
        {
            _configuration = configuration;
            _encryption = encryption;
            _executeWF = executeWF;
            _workflowControl = workflowControl;
        }

        public async Task CreateWebSocket(string tenantId, string domain, int port, string site)
        {
            //var thisSite = IBGlobalConfig.ThisSite ?? string.Empty;

            //var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            //var tenantContext = context.GetTenantContext(tenantId).Context;

            //var webSockets = tenantContext.W_WebSockets.Where(ptr =>
            //    !ptr.IsDelete
            //    && ptr.Site == thisSite
            //    && ptr.Domain == domain
            //    && ptr.Port == port).FirstOrDefault();

            //tenantContext.Context.Dispose();

            var uri = new Uri("ws://localhost:7164/");

            var clientWebSocket = new ClientWebSocket();

            // Connect to the WebSocket server
            try
            {
                await clientWebSocket.ConnectAsync(uri, CancellationToken.None);

                await ReceiveData(clientWebSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to WebSocket server: {ex.Message}");
            }
        }

        private static async Task ReceiveData(ClientWebSocket clientWebSocket)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    var receivedData = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(message);

                    if (receivedData?.Event == "CheckContact")
                    {
                        // Handle CheckContact event data here
                        Console.WriteLine($"Received CheckContact event data: {receivedData.Data}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
            }
        }

        private class Message
        {
            public string? Event { get; set; }
            public string? Data { get; set; }
        }

        //private void ExecuteWorkflow(string tenantId, string wfid)
        //{
        //    var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

        //    var tenantContext = tContext.GetTenantContext(tenantId).Context;

        //    this._executeWF.SetTenantContext(tenantContext);

        //    this._executeWF.Execute(wfid, "{}", this._workflowControl.UpdateDateToUpdate(wfid));

        //    tenantContext.Context.Dispose();
        //}
    }
}