namespace CartService.MessageBroker
{
    public interface IMessageReceiver : IDisposable
    {
        public void ReceiveMessage();
    }

}
