using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Models;

namespace TravelAgencyService.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TravelPackage> TravelPackages { get; set; }
        public DbSet<BookingRule> BookingRules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<WaitingListEntry> WaitingListEntries { get; set; }
        public DbSet<ServiceReview> ServiceReviews { get; set; }

        public DbSet<TravelPackageImage> TravelPackageImages { get; set; }

        public DbSet<TripReview> TripReviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WaitingListEntry>()
                .HasIndex(w => new { w.TravelPackageId, w.Email })
                .IsUnique();

            modelBuilder.Entity<ServiceReview>()
                .HasIndex(r => r.UserId)
                .IsUnique();

        }
    }
}
