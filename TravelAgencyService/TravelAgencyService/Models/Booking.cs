using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgencyService.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

        public DateTime? PaidAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ReminderSentAt { get; set; }

        public decimal PriceAtBooking { get; set; }   
        public decimal? PaidAmount { get; set; }

        public int RoomsCount { get; set; } = 1;

    }
}
