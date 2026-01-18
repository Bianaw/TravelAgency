using System.ComponentModel.DataAnnotations;

namespace TravelAgencyService.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Range(1, 10)]
        public int Quantity { get; set; } = 1; 
}
}
