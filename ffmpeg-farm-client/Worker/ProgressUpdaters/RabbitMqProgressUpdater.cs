using System;
using System.Text;
using FFmpegFarm.Worker.Client;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FFmpegFarm.Worker.ProgressUpdaters
{
    public class RabbitMqProgressUpdater : IProgressUpdater
    {
        private readonly string _queueName;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqProgressUpdater(string dsn, string queueName)
        {
            if (string.IsNullOrWhiteSpace(dsn)) throw new ArgumentNullException(nameof(dsn));
            if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));

            _queueName = queueName;

            IConnectionFactory connectionFactory = new ConnectionFactory
            {
                Uri = dsn
            };
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public void UpdateTask(FFmpegTaskDto task)
        {
            var model = new TaskProgressModel
            {
                MachineName = Environment.MachineName,
                Id = task.Id.GetValueOrDefault(0),
                Progress = TimeSpan.FromSeconds(task.Progress.GetValueOrDefault(0)).ToString("c"),
                VerifyProgress = TimeSpan.FromSeconds(task.VerifyProgress.GetValueOrDefault(0)).ToString("c"),
                Failed = task.State == FFmpegTaskDtoState.Failed,
                Done = task.State == FFmpegTaskDtoState.Done,
                Timestamp = DateTimeOffset.Now
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));

            _channel.BasicPublish(exchange:string.Empty, routingKey: _queueName, basicProperties:null, body: body);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _channel?.Dispose();
        }
    }
}