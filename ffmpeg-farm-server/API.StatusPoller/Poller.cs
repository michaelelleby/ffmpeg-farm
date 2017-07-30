using System;
using System.Data.Common;
using System.Text;
using System.Threading;
using Contract;
using Contract.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace API.StatusPoller
{
    public class Poller : IDisposable
    {
        private readonly IJobRepository _repository;
        private readonly ILogging _logging;
        private readonly string _rabbitMqQueueName;
        private readonly IConnectionFactory _connectionFactory;
        private Thread _thread;
        private bool _shoudStop;
        private IConnection _connection;
        private IModel _channel;

        public Poller(IJobRepository repository, ILogging logging, string rabbitMqDsn, string rabbitMqQueueName)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (string.IsNullOrWhiteSpace(rabbitMqDsn)) throw new ArgumentNullException(nameof(rabbitMqDsn));
            if (string.IsNullOrWhiteSpace(rabbitMqQueueName)) throw new ArgumentNullException(nameof(rabbitMqQueueName));

            _repository = repository;
            _logging = logging;
            _rabbitMqQueueName = rabbitMqQueueName;

            _connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(rabbitMqDsn)
            };
        }

        public void Start()
        {
            _shoudStop = false;

            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(_rabbitMqQueueName, false, false, false, null);

            _thread = new Thread(PollMessages);
            _thread.Start();

        }

        public void Stop()
        {
            _shoudStop = true;

            _thread.Join();

            Dispose();
        }

        private void PollMessages()
        {
            do
            {
                // Get next message
                var message = _channel.BasicGet(_rabbitMqQueueName, false);
                if (message == null)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    continue;
                }

                TaskProgressModel model;
                try
                {
                    var jsonBody = Encoding.UTF8.GetString(message.Body);
                    model = JsonConvert.DeserializeObject<TaskProgressModel>(jsonBody);
                }
                catch (Exception e)
                {
                    _logging.Error(e,
                        $"Caught exception trying to deserialize message from queue. Message deliverytag: {message.DeliveryTag}");

                    // Mark message as dead
                    _channel.BasicReject(message.DeliveryTag, false);

                    continue;
                }

                try
                {
                    _repository.SaveProgress(model.Id, model.Failed, model.Done, model.Progress, model.VerifyProgress,
                        model.MachineName, model.Timestamp);

                    // Acknowledge message received
                    _channel.BasicAck(message.DeliveryTag, false);
                }
                catch (DbException e)
                {
                    _channel.BasicReject(message.DeliveryTag, true);

                    _logging.Error(e, $"Caught exception trying to update task {model.Id}");
                }
            } while (_shoudStop == false);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _channel?.Dispose();
        }
    }
}
