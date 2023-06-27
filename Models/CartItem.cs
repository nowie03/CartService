using System.ComponentModel.DataAnnotations;

namespace CartService.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }

        [Required]
        public int OrderId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
