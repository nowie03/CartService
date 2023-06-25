using System.ComponentModel.DataAnnotations;

namespace CartService.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
