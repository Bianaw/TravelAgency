using System.ComponentModel.DataAnnotations;

namespace TravelAgencyService.Models
{
    public enum PackageType
    {
        Family,
        Honeymoon,
        Adventure,
        Cruise,
        Luxury,
        CityBreak,
        Culture,
        Nature,
        Wellness
    }

    public class TravelPackage
    {
        public int Id { get; set; }

        [Required]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Range(0, 100000)]
        public decimal Price { get; set; }

        [Range(0, 1000)]
        public int AvailableRooms { get; set; }

        [Required]
        public PackageType? PackageType { get; set; }

        [Range(0, 120)]
        public int AgeLimit { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public List<TravelPackageImage> Images { get; set; } = new();

        public string? Image { get; set; }

        [Range(0, 100000)]
        public decimal? OldPrice { get; set; }   

        [DataType(DataType.Date)]
        public DateTime? DiscountStart { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DiscountEnd { get; set; }

        public bool IsVisible { get; set; } = true;

    }
}

