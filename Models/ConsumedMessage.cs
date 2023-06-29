using System.ComponentModel.DataAnnotations;

namespace CartService.Models
{
    public class ConsumedMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        public string ConsumerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public ConsumedMessage(int messageId,string consumerId) {
            MessageId= messageId;
            ConsumerId= consumerId;
        }
    }
}
