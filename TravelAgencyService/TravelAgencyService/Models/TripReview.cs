using System.ComponentModel.DataAnnotations;


namespace TravelAgencyService.Models
{
    public class TripReview
    {
        public int Id { get; set; }

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
