using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;

namespace TravelAgencyService.Controllers
{
    [Authorize]
    public class TripReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TripReviewsController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Create(int packageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var pkgExists = await _context.TravelPackages.AnyAsync(p => p.Id == packageId);
            if (!pkgExists) return NotFound();

            var hasPaidBooking = await _context.Bookings.AnyAsync(b =>
                b.TravelPackageId == packageId &&
                b.UserId == userId &&
                b.Status == BookingStatus.Paid);

            if (!hasPaidBooking)
            {
                TempData["Msg"] = "אפשר לדרג רק אחרי הזמנה ששולמה.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            var already = await _context.TripReviews.AnyAsync(r =>
                r.TravelPackageId == packageId && r.UserId == userId);

            if (already)
            {
                TempData["Msg"] = "כבר דירגת את הטיול הזה.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            ViewBag.PackageId = packageId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int packageId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var pkgExists = await _context.TravelPackages.AnyAsync(p => p.Id == packageId);
            if (!pkgExists) return NotFound();

            var hasPaidBooking = await _context.Bookings.AnyAsync(b =>
                b.TravelPackageId == packageId &&
                b.UserId == userId &&
                b.Status == BookingStatus.Paid);

            if (!hasPaidBooking)
            {
                TempData["Msg"] = "אפשר לדרג רק אחרי הזמנה ששולמה.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            var already = await _context.TripReviews.AnyAsync(r =>
                r.TravelPackageId == packageId && r.UserId == userId);

            if (already)
            {
                TempData["Msg"] = "כבר דירגת את הטיול הזה.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            if (rating < 1 || rating > 5)
            {
                ViewBag.PackageId = packageId;
                ViewBag.Error = "דירוג חייב להיות בין 1 ל־5.";
                return View(); 
            }

            _context.TripReviews.Add(new TripReview
            {
                TravelPackageId = packageId,
                UserId = userId,
                Rating = rating,
                Comment = (comment ?? "").Trim(),
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Msg"] = "תודה! הדירוג נשמר.";
            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
        }
    }
}
