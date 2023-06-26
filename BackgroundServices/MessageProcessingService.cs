using CartService.MessageBroker;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;

namespace CartService.BackgroundServices
{
    public class MessageProcessingService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Guid, IMessageBrokerClient> _messageBrokerClients;
        private Guid _scopeKey;


        public MessageProcessingService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _messageBrokerClients = new ConcurrentDictionary<Guid, IMessageBrokerClient>();
            _scopeKey = Guid.NewGuid();


        }

        private IMessageBrokerClient GetScopedMessageBrokerClient(IServiceProvider serviceProvider)
        {
            if (!_messageBrokerClients.TryGetValue(_scopeKey, out var messageBrokerClient))
            {
                // Create and cache the scoped message broker client
                messageBrokerClient = serviceProvider.GetRequiredService<IMessageBrokerClient>();
                _messageBrokerClients.TryAdd(_scopeKey, messageBrokerClient);
            }

            return messageBrokerClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform any additional background processing if needed
                try
                {
                    var scope = _serviceProvider.CreateScope();

                    var messageBrokerClient = GetScopedMessageBrokerClient(scope.ServiceProvider);
                    messageBrokerClient.ReceiveMessage();


                }
                catch (AlreadyClosedException ex)
                {
                    Console.WriteLine("unable to connect to queue");


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                await Task.Delay(1000, stoppingToken); // Delay between iterations to avoid high CPU usage
            }


        }
    }
}

