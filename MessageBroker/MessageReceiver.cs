
using CartService.Constants;
using CartService.Context;
using CartService.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace CartService.MessageBroker
{
    public class MessageReceiver : IMessageReceiver, IDisposable
    {
        private IConnectionFactory _connectionFactory;
        private IModel _channel;
        private IServiceProvider _serviceProvider;
        private readonly string _queueName = "service-queue";
        private EventingBasicConsumer _consumer;
        public static int instance=0;
        public MessageReceiver(IServiceProvider serviceProvider)
        {
            Console.WriteLine($"created message receiver instance {instance}");
            _serviceProvider = serviceProvider;
            _connectionFactory = new ConnectionFactory
            {
                HostName = "message-queue"
            };

            try
            {

                //Create the RabbitMQ connection using connection factory details as i mentioned above
                var connection = _connectionFactory.CreateConnection();
                //Here we create channel with session and model
                _channel = connection.CreateModel();
                //declare the queue after mentioning name and a few property related to that
                //_channel.QueueDeclare(_queueName, exclusive: false);
                _consumer = new EventingBasicConsumer(_channel);

                _channel.ConfirmSelect();

                _channel.BasicAcks += async (sender, ea) => await HandleMessageAcknowledge(ea.DeliveryTag, ea.Multiple);
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        private async Task HandleMessageAcknowledge(ulong currentSequenceNumber, bool multiple)
        {
            try
            {
                var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServiceContext>();
                if (multiple)
                {

                    await dbContext.Outbox
                        .Where(message => message.SequenceNumber <= currentSequenceNumber)
                        .ExecuteUpdateAsync(
                        entity => entity.SetProperty(
                            message => message.State,
                            Constants.EventStates.EVENT_ACK_COMPLETED
                            )
                        );


                }
                else
                {
                    Message? messageToBeUpdated = await dbContext.Outbox.FirstOrDefaultAsync(message => message.SequenceNumber == currentSequenceNumber);
                    if (messageToBeUpdated != null)
                    {
                        messageToBeUpdated.State = EventStates.EVENT_ACK_COMPLETED;
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void ReceiveMessage()
        {
            if (_channel == null)
                return;
            Console.WriteLine("waiting for message");



            _consumer.Received += async (model, eventArgs) => await HandleMessageReceived(eventArgs);


            //read the message
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: _consumer);




        }

        private async Task HandleMessageReceived(BasicDeliverEventArgs eventArgs)
        {


            using var handlerScope = _serviceProvider.CreateScope();
            var _serviceContext = handlerScope.ServiceProvider.GetRequiredService<ServiceContext>();
            
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"message received from queue {message}");

            //check if this message is already consumed if no process it
            Message? eventMessage = JsonConvert.DeserializeObject<Message>(message);

            

            if (eventMessage != null
                && (eventMessage.EventType == EventTypes.USER_CREATED || eventMessage.EventType == EventTypes.USER_DELETED))
            {

                string consumerId = "cart-service";

                bool alreadyProcessed = await _serviceContext.ConsumedMessages.AnyAsync(message =>
                message.MessageId == eventMessage.Id && message.ConsumerId == consumerId);

                if (alreadyProcessed) return;


                ConsumedMessage consumedMessage = new ConsumedMessage(eventMessage.Id, consumerId);

                try
                {
                    await _serviceContext.ConsumedMessages.AddAsync(consumedMessage);
                    await _serviceContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            // Perform the message handling logic here based on the event message
            if (eventMessage != null && eventMessage.EventType == EventTypes.USER_CREATED)
            {
                // Handle the USER_CREATED event
                User userAdded = JsonConvert.DeserializeObject<User>(eventMessage.Payload);

                try
                {
                    Cart cart = new()
                    {
                        UserId = userAdded.Id,
                        CreatedAt = DateTime.Now
                    };

                    await _serviceContext.Carts.AddAsync(cart);
                    await _serviceContext.SaveChangesAsync();

                    _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error when adding cart on user addition in message handler {ex.Message}");
                }

                //acknowldege queue of successful consume 
            }


            if (eventMessage != null && eventMessage.EventType == EventTypes.USER_DELETED)
            {
                // Handle the USER_DELETED event
                // ...
                User userDeleted = JsonConvert.DeserializeObject<User>(eventMessage.Payload);

                try
                {
                    Cart? cart = _serviceContext.Carts.Where(cart => cart.UserId == userDeleted.Id).FirstOrDefault() ??
                        throw new Exception($"cart cannot be found for user id {userDeleted.Id}");

                    _serviceContext.Carts.Remove(cart);
                    await _serviceContext.SaveChangesAsync();

                    _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error occured when deleting cart for user id {userDeleted.Id}");
                }
                // Acknowledge the message
            }
            Console.WriteLine("scope disposed for handle message receiver");

           
        }

        public void Dispose()
        {
            Console.WriteLine($"Disposed mesage receiver {instance}");
            _channel.Dispose();
        }
    }
}
