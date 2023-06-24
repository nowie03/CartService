namespace CartService.Models
{
    public class Message<T>
    {

        public string EventType { get; set; }

        public T Payload { get; set; }

        public Message(string eventType, T payload)
        {
            EventType = eventType;
            Payload = payload;
        }
    }
}
