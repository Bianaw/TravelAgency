using System.ComponentModel.DataAnnotations;

namespace TravelAgencyService.Models
{
    public class WaitingListEntry
    {
        public int Id { get; set; }

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool Notified { get; set; } = false; 
    }
}

