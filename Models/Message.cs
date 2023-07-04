using System.ComponentModel.DataAnnotations;

namespace CartService.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string EventType { get; set; }

        [Required]
        public string Payload { get; set; }

        [Required]
        public ulong SequenceNumber { get; set; }


        [Required]
        public string State { get; set; }

        public DateTime CreatedAt { get; set; }


        public Message(string eventType, string payload, ulong sequenceNumber, string state)
        {
            EventType = eventType;
            Payload = payload;
            SequenceNumber = sequenceNumber;
            State = state;
        }
    }
}
