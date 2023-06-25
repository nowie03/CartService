using CartService.Constants;
using CartService.Context;
using CartService.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace CartService.MessageBroker
{
    public class MessageHandler<T> where T : User
    {
        private  readonly IModel _channel;
    
        private readonly IServiceProvider _serviceProvider;

        public MessageHandler(IModel channel ,IServiceProvider serviceProvider)
        {
            //get servicecontext from injected service container
            _serviceProvider = serviceProvider;

            _channel = channel;

            Console.WriteLine("message handler created");
        }

        public async void HandleMessage(object model, BasicDeliverEventArgs eventArgs)
        {
            using var scope = _serviceProvider.CreateScope();
            var _serviceContext = scope.ServiceProvider.GetRequiredService<ServiceContext>();

            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"message received from queue {message}");

            Message<T> eventMessage = JsonConvert.DeserializeObject<Message<T>>(message);

            // Perform the message handling logic here based on the event message
            if (eventMessage.EventType == EventTypes.USER_CREATED)
            {
                // Handle the USER_CREATED event
                User userAdded = eventMessage.Payload;

                try
                {
                    Cart cart = new()
                    {
                        UserId = userAdded.Id,
                        CreatedAt = DateTime.Now
                    };

                    await _serviceContext.Cart.AddAsync(cart);
                    await _serviceContext.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error when adding cart on user addition in message handler {ex.Message}");
                }

                //acknowldege queue of successful consume 
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }


            if (eventMessage.EventType == EventTypes.USER_DELETED)
            {
                // Handle the USER_DELETED event
                // ...
                User userDeleted = eventMessage.Payload;

                try
                {
                    Cart? cart = _serviceContext.Cart.Where(cart => cart.UserId == userDeleted.Id).FirstOrDefault() ??
                        throw new Exception($"cart cannot be found for user id {userDeleted.Id}");

                    _serviceContext.Remove(cart);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error occured when deleting cart for user id {userDeleted.Id}");
                }
                // Acknowledge the message
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }

        }
    }
}
