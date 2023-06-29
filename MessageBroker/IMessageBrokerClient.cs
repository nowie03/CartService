using CartService.Models;

namespace CartService.MessageBroker
{
    public interface IMessageBrokerClient
    {
        public void SendMessage(Message message);

        public void ReceiveMessage();

        public ulong GetNextSequenceNumber();
        
    }
}
