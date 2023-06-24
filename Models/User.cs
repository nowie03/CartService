using System.ComponentModel.DataAnnotations;

namespace CartService.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public String FirstName { get; set; }

        [Required]
        public String LastName { get; set; }

        [Required]
        public String Username { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
