using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;

namespace TravelAgencyService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tripsCount = await _context.TravelPackages.CountAsync(p => p.IsVisible);

            var reviewsQ = _context.ServiceReviews.Include(r => r.User).AsQueryable();
            var reviewsCount = await reviewsQ.CountAsync();
            var avgRating = reviewsCount > 0 ? await reviewsQ.AverageAsync(r => r.Rating) : 0.0;

            var recent = await reviewsQ
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.TripsCount = tripsCount;
            ViewBag.ServiceReviewsCount = reviewsCount;
            ViewBag.ServiceAvgRating = avgRating;
            ViewBag.ServiceRecent = recent;

            bool canLeave = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var hasPaid = await _context.Bookings.AnyAsync(b =>
                    b.UserId == userId && b.Status == BookingStatus.Paid);

                var already = await _context.ServiceReviews.AnyAsync(r => r.UserId == userId);

                canLeave = hasPaid && !already;
            }
            ViewBag.CanLeaveServiceReview = canLeave;

            var today = DateTime.Today;

            var visibleTripsQ = _context.TravelPackages
                .AsNoTracking()
                .Where(p => p.IsVisible);
            var popularIds = await _context.TripReviews
                .GroupBy(r => r.TravelPackageId)
                .Select(g => new { Id = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Take(3)
                .ToListAsync();

            var popularIdList = popularIds.Select(x => x.Id).ToList();

            var popularTripsRaw = await visibleTripsQ
                .Where(p => popularIdList.Contains(p.Id))
                .ToListAsync();

            var popularTrips = popularIdList
                .Join(popularTripsRaw, id => id, p => p.Id, (id, p) => p)
                .ToList();

            if (popularTrips.Count < 3)
            {
                var need = 3 - popularTrips.Count;
                var fill = await visibleTripsQ
                    .Where(p => !popularTrips.Select(x => x.Id).Contains(p.Id))
                    .OrderByDescending(p => p.StartDate)
                    .Take(need)
                    .ToListAsync();

                popularTrips.AddRange(fill);
            }

            var saleTrips = await visibleTripsQ
                .Where(p =>
                    p.OldPrice.HasValue &&
                    p.DiscountStart.HasValue &&
                    p.DiscountEnd.HasValue &&
                    today >= p.DiscountStart.Value.Date &&
                    today <= p.DiscountEnd.Value.Date
                )
                .OrderBy(p => p.DiscountEnd) 
                .Take(4)
                .ToListAsync();

            var ids = popularTrips.Select(p => p.Id)
                .Concat(saleTrips.Select(p => p.Id))
                .Distinct()
                .ToList();

            var stats = await _context.TripReviews
                .Where(r => ids.Contains(r.TravelPackageId))
                .GroupBy(r => r.TravelPackageId)
                .Select(g => new
                {
                    TravelPackageId = g.Key,
                    Avg = g.Average(x => x.Rating),
                    Cnt = g.Count()
                })
                .ToListAsync();

            ViewBag.PopularTrips = popularTrips;
            ViewBag.SaleTrips = saleTrips;
            ViewBag.TripAvgRatings = stats.ToDictionary(x => x.TravelPackageId, x => x.Avg);
            ViewBag.TripReviewCounts = stats.ToDictionary(x => x.TravelPackageId, x => x.Cnt);

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
