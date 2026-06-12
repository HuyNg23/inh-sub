using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace IBox.Common.TCP
{
    public class DefaultKafkaProducer : IKafkaProducer
    {
        public async Task ProduceMessageAsync(string bootstrapServers, string topic, string messageBody, string partitionKey)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config)
                .SetValueSerializer(Serializers.Utf8)
                .SetKeySerializer(Serializers.Utf8)
                .Build();

            var message = new Message<string, string>
            {
                Key = string.IsNullOrEmpty(partitionKey) ? null : partitionKey,
                Value = messageBody
            };

            var deliveryResult = await producer.ProduceAsync(topic, message).ConfigureAwait(false);
            producer.Flush(TimeSpan.FromSeconds(10));
        }

        public async Task ProduceMessageWithHeadersAsync(string bootstrapServers, string topic, string messageBody, Dictionary<string, string> headers, string partitionKey)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config)
                .SetValueSerializer(Serializers.Utf8)
                .SetKeySerializer(Serializers.Utf8)
                .Build();

            var message = new Message<string, string>
            {
                Key = string.IsNullOrEmpty(partitionKey) ? null : partitionKey,
                Value = messageBody,
                Headers = new Headers()
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (string.IsNullOrEmpty(header.Key))
                    {
                        continue;
                    }

                    message.Headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value ?? string.Empty));
                }
            }

            var deliveryResult = await producer.ProduceAsync(topic, message).ConfigureAwait(false);
            producer.Flush(TimeSpan.FromSeconds(10));
        }
    }
}
