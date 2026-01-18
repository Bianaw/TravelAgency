using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;

namespace TravelAgencyService.Controllers
{
    [Authorize]
    public class ServiceReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ServiceReviewsController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (rating < 1 || rating > 5)
            {
                TempData["SrvMsg"] = "דירוג חייב להיות בין 1 ל־5.";
                return RedirectToAction("Index", "Home");
            }

            // רק אחרי הזמנה ששולמה 
            var hasPaid = await _context.Bookings.AnyAsync(b =>
                b.UserId == userId && b.Status == BookingStatus.Paid);

            if (!hasPaid)
            {
                TempData["SrvMsg"] = "אפשר להשאיר פידבק רק אחרי הזמנה ששולמה.";
                return RedirectToAction("Index", "Home");
            }

            var already = await _context.ServiceReviews.AnyAsync(r => r.UserId == userId);
            if (already)
            {
                TempData["SrvMsg"] = "כבר שלחת פידבק על השירות.";
                return RedirectToAction("Index", "Home");
            }

            _context.ServiceReviews.Add(new ServiceReview
            {
                UserId = userId,
                Rating = rating,
                Comment = (comment ?? "").Trim()
            });

            await _context.SaveChangesAsync();
            TempData["SrvMsg"] = "תודה! הפידבק נשמר.";
            return RedirectToAction("Index", "Home");
        }
    }
}
