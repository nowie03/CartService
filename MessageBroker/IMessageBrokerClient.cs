namespace CartService.MessageBroker
{
    public interface IMessageBrokerClient
    {
        public void SendMessage<T>(T message, string eventType);

        public void ReceiveMessage<T>();
    }
}
