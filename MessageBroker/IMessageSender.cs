using CartService.Models;

namespace CartService.MessageBroker
{
    public interface IMessageSender
    {
        public void SendMessage(Message message);

       

        public ulong GetNextSequenceNumber();
        
    }
}
