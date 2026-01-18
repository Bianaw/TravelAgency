using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;

namespace TravelAgencyService.Services
{
    public class ReminderEmailHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ReminderEmailHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var rule = await db.BookingRules
                        .OrderByDescending(r => r.Id)
                        .FirstOrDefaultAsync(stoppingToken);

                    var reminderDays = rule?.ReminderDaysBeforeStart ?? 5;

                    var targetDate = DateTime.Today.AddDays(reminderDays);

                    var bookings = await db.Bookings
                        .Include(b => b.TravelPackage)
                        .Where(b =>
                            b.Status == BookingStatus.Paid &&
                            b.ReminderSentAt == null &&
                            b.TravelPackage != null &&
                            b.TravelPackage.StartDate.Date == targetDate)
                        .ToListAsync(stoppingToken);

                    foreach (var booking in bookings)
                    {
                        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == booking.UserId, stoppingToken);
                        var toEmail = (user?.Email ?? "").Trim();

                        if (string.IsNullOrWhiteSpace(toEmail))
                            continue;

                        var pkg = booking.TravelPackage!;
                        var subject = $"Reminder: Your trip to {pkg.Destination} starts in {reminderDays} day(s)";
                        var body = $@"
                            <h2>Trip Reminder ⏳</h2>
                            <p>This is a reminder that your trip is coming soon.</p>
                            <p><b>Destination:</b> {pkg.Destination} ({pkg.Country})</p>
                            <p><b>Dates:</b> {pkg.StartDate:dd/MM/yyyy} - {pkg.EndDate:dd/MM/yyyy}</p>
                            <p><b>Booking #:</b> {booking.Id}</p>
                            <p>Have a great trip!</p>";

                        await emailSender.SendAsync(toEmail, subject, body);

                        booking.ReminderSentAt = DateTime.Now;
                    }

                    if (bookings.Count > 0)
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
