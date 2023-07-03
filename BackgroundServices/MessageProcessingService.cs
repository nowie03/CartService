using CartService.MessageBroker;
using Microsoft.OpenApi.Writers;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;

namespace CartService.BackgroundServices
{
    public class MessageProcessingService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Guid, IServiceScope> _cachedScope;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private Guid _scopeKey;


        public MessageProcessingService(IConfiguration configuration, IServiceProvider serviceProvider,IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            serviceScopeFactory = scopeFactory;
           _cachedScope = new ConcurrentDictionary<Guid, IServiceScope>();
            _scopeKey = Guid.NewGuid();


        }

        private IServiceScope GetCachedScope(IServiceScopeFactory scopeFactory)
        {
            if (!_cachedScope.TryGetValue(_scopeKey, out var scope))
            {
                // Create and cache the scope
                var newScope = scopeFactory.CreateScope();
                _cachedScope.TryAdd(_scopeKey, newScope);

                return newScope;
            }

            return scope;
        }

        

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform any additional background processing if needed
                try
                {
                    //check if there is a valid scope
                    var scope = GetCachedScope(serviceScopeFactory);
                    var messageReceiver = scope.ServiceProvider.GetRequiredService<IMessageReceiver>();
                    messageReceiver.ReceiveMessage();
                  
                    

                }
                catch (AlreadyClosedException ex)
                {
                   
                    _scopeKey=Guid.NewGuid();
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

