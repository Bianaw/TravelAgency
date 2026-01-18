using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using TravelAgencyService.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;








namespace TravelAgencyService.Controllers
{
    public class TravelPackagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;

        public TravelPackagesController(ApplicationDbContext context, IEmailSender emailSender, IWebHostEnvironment env)
        {
            _context = context;
            _emailSender = emailSender;
            _env = env;

        }





        [AllowAnonymous]
        public async Task<IActionResult> Index(
    PackageType? type,
    decimal? minPrice,
    decimal? maxPrice,
    DateTime? start,
    DateTime? end,
    string? search,
    string? sort,
    bool? onSale)
        {
            var q = _context.TravelPackages.AsQueryable();

            if (!User.IsInRole("Admin"))
                q = q.Where(p => p.IsVisible);

            if (type.HasValue)
                q = q.Where(p => p.PackageType == type);

            if (minPrice.HasValue)
                q = q.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                q = q.Where(p => p.Price <= maxPrice.Value);

            if (start.HasValue)
                q = q.Where(p => p.StartDate.Date >= start.Value.Date);

            if (end.HasValue)
                q = q.Where(p => p.EndDate.Date <= end.Value.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                q = q.Where(p =>
                    p.Destination.Contains(search) ||
                    p.Country.Contains(search) ||
                    p.Description.Contains(search)
                );
            }
            if (onSale == true)
            {
                var today = DateTime.Today;
                q = q.Where(p =>
                    p.OldPrice.HasValue &&
                    p.DiscountStart.HasValue &&
                    p.DiscountEnd.HasValue &&
                    today >= p.DiscountStart.Value.Date &&
                    today <= p.DiscountEnd.Value.Date
                );
            }

            ViewBag.OnSale = onSale == true;

            if (sort == "popular")
            {
                q = q
                    .GroupJoin(
                        _context.TripReviews,
                        p => p.Id,
                        r => r.TravelPackageId,
                        (p, reviews) => new { P = p, Cnt = reviews.Count() }
                    )
                    .OrderByDescending(x => x.Cnt)
                    .ThenBy(x => x.P.Destination)
                    .Select(x => x.P);
            }
            else
            {
                q = sort switch
                {
                    "price_asc" => q.OrderBy(p => p.Price),
                    "price_desc" => q.OrderByDescending(p => p.Price),
                    "date_asc" => q.OrderBy(p => p.StartDate),
                    "date_desc" => q.OrderByDescending(p => p.StartDate),
                    "type" => q.OrderBy(p => p.PackageType),
                    _ => q.OrderBy(p => p.Destination) // Default
                };
            }

            ViewBag.Type = type;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Start = start?.ToString("yyyy-MM-dd");
            ViewBag.End = end?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            var packages = await q.AsNoTracking().ToListAsync();

            var ids = packages.Select(p => p.Id).ToList();

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

            ViewBag.AvgRatings = stats.ToDictionary(x => x.TravelPackageId, x => x.Avg);
            ViewBag.ReviewCounts = stats.ToDictionary(x => x.TravelPackageId, x => x.Cnt);

            return View(packages);
        }





        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pkg = await _context.TravelPackages
    .Include(p => p.Images)
    .FirstOrDefaultAsync(p => p.Id == id);

            var travelPackage = await _context.TravelPackages.FirstOrDefaultAsync(m => m.Id == id);
            if (travelPackage == null) return NotFound();

            if (!User.IsInRole("Admin") && !travelPackage.IsVisible)
                return NotFound();

            var reviewsQ = _context.TripReviews
                .Include(r => r.User)
                .Where(r => r.TravelPackageId == id);

            ViewBag.ReviewsCount = await reviewsQ.CountAsync();
            ViewBag.AvgRating = (ViewBag.ReviewsCount > 0)
                ? await reviewsQ.AverageAsync(r => r.Rating)
                : 0.0;

            ViewBag.RecentReviews = await reviewsQ
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            bool canReview = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var hasPaidBooking = await _context.Bookings.AnyAsync(b =>
                    b.TravelPackageId == id && b.UserId == userId && b.Status == BookingStatus.Paid);

                var already = await _context.TripReviews.AnyAsync(r =>
                    r.TravelPackageId == id && r.UserId == userId);

                canReview = hasPaidBooking && !already;
            }

            ViewBag.CanReview = canReview;

            return View(travelPackage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddReview(int travelPackageId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (rating < 1 || rating > 5)
            {
                TempData["Msg"] = "Rating must be between 1 and 5.";
                return RedirectToAction(nameof(Details), new { id = travelPackageId });
            }

            var hasPaidBooking = await _context.Bookings.AnyAsync(b =>
                b.TravelPackageId == travelPackageId &&
                b.UserId == userId &&
                b.Status == BookingStatus.Paid);

            if (!hasPaidBooking)
            {
                TempData["Msg"] = "You can review only after a paid booking.";
                return RedirectToAction(nameof(Details), new { id = travelPackageId });
            }

            var already = await _context.TripReviews.AnyAsync(r =>
                r.TravelPackageId == travelPackageId && r.UserId == userId);

            if (already)
            {
                TempData["Msg"] = "You already reviewed this trip.";
                return RedirectToAction(nameof(Details), new { id = travelPackageId });
            }

            _context.TripReviews.Add(new TripReview
            {
                TravelPackageId = travelPackageId,
                UserId = userId,
                Rating = rating,
                Comment = (comment ?? "").Trim(),
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Msg"] = "Thanks for your review!";
            return RedirectToAction(nameof(Details), new { id = travelPackageId });
        }




        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(TravelPackage travelPackage, IFormFile? imageFile, string? tempImagesJson)
        {
            if (ModelState.IsValid)
            {
                if (travelPackage.DiscountStart.HasValue || travelPackage.DiscountEnd.HasValue || travelPackage.OldPrice.HasValue)
                {
                    if (!travelPackage.DiscountStart.HasValue || !travelPackage.DiscountEnd.HasValue || !travelPackage.OldPrice.HasValue)
                    {
                        ModelState.AddModelError("", "If you set a discount, you must provide OldPrice + DiscountStart + DiscountEnd.");
                        return View(travelPackage);
                    }

                    if (travelPackage.DiscountEnd < travelPackage.DiscountStart)
                    {
                        ModelState.AddModelError("", "DiscountEnd must be after DiscountStart.");
                        return View(travelPackage);
                    }

                    if ((travelPackage.DiscountEnd.Value - travelPackage.DiscountStart.Value).TotalDays > 7)
                    {
                        ModelState.AddModelError("", "Discount can be active for up to 7 days only.");
                        return View(travelPackage);
                    }

                    if (travelPackage.OldPrice <= travelPackage.Price)
                    {
                        ModelState.AddModelError("", "OldPrice must be greater than the current Price.");
                        return View(travelPackage);
                    }
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/images/packages");

                    Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString()
                        + Path.GetExtension(imageFile.FileName);

                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    travelPackage.Image = fileName;
                }

                _context.Add(travelPackage);
                await _context.SaveChangesAsync();
                if (!string.IsNullOrWhiteSpace(tempImagesJson))
                {
                    var tempList = JsonSerializer.Deserialize<List<TempImg>>(tempImagesJson) ?? new();

                    foreach (var t in tempList)
                    {
                        if (string.IsNullOrWhiteSpace(t.fileName)) continue;

                        var srcAbs = Path.Combine(_env.WebRootPath, "uploads", "temp", t.fileName);
                        if (!System.IO.File.Exists(srcAbs)) continue;

                        var destRelFolder = Path.Combine("uploads", "packages", travelPackage.Id.ToString());
                        var destAbsFolder = Path.Combine(_env.WebRootPath, destRelFolder);
                        Directory.CreateDirectory(destAbsFolder);

                        var destAbs = Path.Combine(destAbsFolder, t.fileName);

                        System.IO.File.Move(srcAbs, destAbs, true);

                        var url = "/" + Path.Combine(destRelFolder, t.fileName).Replace("\\", "/");

                        _context.TravelPackageImages.Add(new TravelPackageImage
                        {
                            TravelPackageId = travelPackage.Id,
                            Url = url
                        });
                    }

                    await _context.SaveChangesAsync();
                }


                return RedirectToAction(nameof(Index));
            }

            return View(travelPackage);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var travelPackage = await _context.TravelPackages
     .Include(p => p.Images)
     .FirstOrDefaultAsync(p => p.Id == id);

            if (travelPackage == null) return NotFound();

            return View(travelPackage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, TravelPackage formModel, IFormFile? imageFile)
        {
            if (id != formModel.Id) return NotFound();

            var package = await _context.TravelPackages.FindAsync(id);
            if (package == null) return NotFound();
            var oldRooms = package.AvailableRooms;


            package.Destination = formModel.Destination;
            package.Country = formModel.Country;
            package.StartDate = formModel.StartDate;
            package.EndDate = formModel.EndDate;
            package.Price = formModel.Price;
            package.AvailableRooms = formModel.AvailableRooms;
            package.PackageType = formModel.PackageType;
            package.AgeLimit = formModel.AgeLimit;
            package.Description = formModel.Description;

            package.OldPrice = formModel.OldPrice;
            package.DiscountStart = formModel.DiscountStart;
            package.DiscountEnd = formModel.DiscountEnd;
            package.IsVisible = formModel.IsVisible;

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/packages");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(package.Image))
                {
                    var oldPath = Path.Combine(uploadsFolder, package.Image);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                package.Image = fileName;
            }

            await _context.SaveChangesAsync();

            var newRooms = package.AvailableRooms - oldRooms;
            if (newRooms > 0)
            {
                await NotifyWaitingListAsync(package.Id, newRooms);
            }

            return RedirectToAction(nameof(Index));
        }




        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var travelPackage = await _context.TravelPackages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (travelPackage == null) return NotFound();

            return View(travelPackage);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var travelPackage = await _context.TravelPackages.FindAsync(id);
            if (travelPackage != null)
            {
                _context.TravelPackages.Remove(travelPackage);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        private async Task NotifyWaitingListAsync(int packageId, int roomsToNotify)
        {
            var pkg = await _context.TravelPackages.FindAsync(packageId);
            if (pkg == null) return;

            var entries = await _context.WaitingListEntries
                .Where(w => w.TravelPackageId == packageId && !w.Notified)
                .OrderBy(w => w.CreatedAt).ThenBy(w => w.Id)
                .Take(roomsToNotify)
                .ToListAsync();

            foreach (var entry in entries)
            {
                var subject = $"Room available for {pkg.Destination} ({pkg.Country})";
                var body = $@"
            <h2>Good news!</h2>
            <p>A room is now available for:</p>
            <p><b>{pkg.Destination}</b>, {pkg.Country}</p>
            <p>Dates: {pkg.StartDate:dd/MM/yyyy} - {pkg.EndDate:dd/MM/yyyy}</p>
            <p>Please visit the website to book as soon as possible.</p>";

                await _emailSender.SendAsync(entry.Email, subject, body);

                entry.Notified = true;
            }

            await _context.SaveChangesAsync();
        }

        private bool TravelPackageExists(int id)
        {
            return _context.TravelPackages.Any(e => e.Id == id);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CheckRooms(int packageId, int qty)
        {
            qty = Math.Max(1, qty);

            var pkg = await _context.TravelPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (pkg == null)
                return Json(new { ok = false, available = 0, message = "Trip not found." });

            if (!User.IsInRole("Admin") && !pkg.IsVisible)
                return Json(new { ok = false, available = 0, message = "Trip not found." });

            if (pkg.AvailableRooms >= qty)
                return Json(new { ok = true, available = pkg.AvailableRooms, message = $"✅ Available: {pkg.AvailableRooms}. You can book {qty} room(s)." });

            return Json(new { ok = false, available = pkg.AvailableRooms, message = $"❌ Only {pkg.AvailableRooms} room(s) left." });
        }

        public class TempImg
        {
            public string fileName { get; set; } = "";
            public string url { get; set; } = "";
        }

    }
}

