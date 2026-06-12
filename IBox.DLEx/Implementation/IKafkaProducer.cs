using System.Collections.Generic;
using System.Threading.Tasks;

namespace IBox.Common.TCP
{
    public interface IKafkaProducer
    {
        Task ProduceMessageWithHeadersAsync(string bootstrapServers, string topic, string messageBody, Dictionary<string, string> headers, string partitionKey);
        Task ProduceMessageAsync(string bootstrapServers, string topic, string messageBody, string partitionKey);
    }
}
