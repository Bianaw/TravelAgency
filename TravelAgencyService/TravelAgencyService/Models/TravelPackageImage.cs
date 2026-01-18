using System.ComponentModel.DataAnnotations;


namespace TravelAgencyService.Models
{
    public class TravelPackageImage
    {
        public int Id { get; set; }

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage TravelPackage { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = "";
    }
}
