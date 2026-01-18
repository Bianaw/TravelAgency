using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;

namespace TravelAgencyService.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BookingRulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingRulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // נשמור תמיד רשומה אחת
            var rule = await _context.BookingRules.FirstOrDefaultAsync();
            if (rule == null)
            {
                rule = new BookingRule();
                _context.BookingRules.Add(rule);
                await _context.SaveChangesAsync();
            }
            return View(rule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(BookingRule model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var rule = await _context.BookingRules.FirstOrDefaultAsync();
            if (rule == null)
            {
                _context.BookingRules.Add(model);
            }
            else
            {
                rule.LatestBookingDaysBeforeStart = model.LatestBookingDaysBeforeStart;
                rule.CancellationDaysBeforeStart = model.CancellationDaysBeforeStart;
                rule.ReminderDaysBeforeStart = model.ReminderDaysBeforeStart;
                rule.MaxActiveBookings = model.MaxActiveBookings;
            }

            await _context.SaveChangesAsync();
            TempData["RuleMsg"] = "Rules saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
