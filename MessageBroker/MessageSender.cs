using CartService.Constants;
using CartService.Context;
using CartService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace CartService.MessageBroker
{
    public class MessageSender : IMessageSender, IDisposable
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName = "service-queue";
        private readonly IServiceProvider _serviceProvider;

        
        //create Dbcontext 

        public MessageSender(IServiceProvider serviceProvider)
        {


            SetupClient(serviceProvider);
            _serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposed current client");   
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        private void SetupClient(IServiceProvider serviceProvider)
        {
            //Here we specify the Rabbit MQ Server. we use rabbitmq docker image and use it
            _connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            try
            {
                
                //Create the RabbitMQ connection using connection factory details as i mentioned above
                _connection = _connectionFactory.CreateConnection();
                //Here we create channel with session and model
                _channel = _connection.CreateModel();
                //declare the queue after mentioning name and a few property related to that
                //_channel.QueueDeclare(_queueName, exclusive: false);

               

                _channel.ConfirmSelect();

                _channel.BasicAcks += (sender, ea) => HandleMessageAcknowledge(ea.DeliveryTag, ea.Multiple);
            }
            catch(BrokerUnreachableException ex)
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendMessage(Message message)
        {

            //Serialize the message
            if (_channel == null)
                return;

            

            string json = JsonConvert.SerializeObject(message);


            var body = Encoding.UTF8.GetBytes(json);

           
            //put the data on to the product queue
            _channel.BasicPublish(exchange: "", routingKey: _queueName, body: body);
        }


      

       
        public ulong GetNextSequenceNumber()
        {
            return _channel.NextPublishSeqNo;
        }
    }
}
