using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;
using Microsoft.AspNetCore.Authorization;
namespace TravelAgencyService.Controllers;
using TravelAgencyService.Services;


public class WaitingListController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;

    public WaitingListController(ApplicationDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int packageId)
    {
        var email = (User.Identity?.Name ?? "").Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["WaitMsg"] = "Please login with an email to join the waiting list.";
            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
        }

        var pkg = await _context.TravelPackages.FindAsync(packageId);
        if (pkg == null) return NotFound();

        var hasAnyList = await _context.WaitingListEntries.AnyAsync(w => w.TravelPackageId == packageId);
        if (pkg.AvailableRooms > 0 && !hasAnyList)
        {
            TempData["WaitMsg"] = "Rooms are available now. No need to join the waiting list.";
            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
        }

        var existing = await _context.WaitingListEntries
            .FirstOrDefaultAsync(w => w.TravelPackageId == packageId && w.Email == email);

        if (existing != null)
        {
            if (!existing.Notified)
            {
                var position = await GetPositionAsync(existing);
                TempData["WaitMsg"] = $"You are already on the waiting list. Your position: {position}.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            existing.Notified = false;
            existing.CreatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var newPos = await GetPositionAsync(existing);
            TempData["WaitMsg"] = $"You have been re-added to the waiting list. Your position: {newPos}.";
            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
        }

        var entry = new WaitingListEntry
        {
            TravelPackageId = packageId,
            Email = email,
            CreatedAt = DateTime.Now,
            Notified = false
        };

        _context.WaitingListEntries.Add(entry);
        await _context.SaveChangesAsync();

        var pos = await GetPositionAsync(entry);
        TempData["WaitMsg"] = $"You have been added to the waiting list. Your position: {pos}.";
        return RedirectToAction("Details", "TravelPackages", new { id = packageId });
    }


    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminList()
    {
        var entries = await _context.WaitingListEntries
            .Include(w => w.TravelPackage)
            .OrderBy(w => w.TravelPackageId)
            .ThenBy(w => w.Notified)         
            .ThenBy(w => w.CreatedAt)
            .ThenBy(w => w.Id)
            .ToListAsync();

        var positions = new Dictionary<int, int>();
        int currentPackageId = -1;
        int pos = 0;

        foreach (var e in entries)
        {
            if (e.TravelPackageId != currentPackageId)
            {
                currentPackageId = e.TravelPackageId;
                pos = 0;
            }

            if (!e.Notified)
            {
                pos++;
                positions[e.Id] = pos;
            }
        }

        ViewBag.Positions = positions;

        return View(entries);
    }


   
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkNotified(int id)
    {
        var entry = await _context.WaitingListEntries.FindAsync(id);
        if (entry == null) return NotFound();

        entry.Notified = true;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(AdminList));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> NotifyNext(int packageId)
    {
        var pkg = await _context.TravelPackages.FindAsync(packageId);
        if (pkg == null) return NotFound();

        if (pkg.AvailableRooms <= 0)
        {
            TempData["WaitMsg"] = "No available rooms yet. Can't notify anyone.";
            return RedirectToAction(nameof(AdminList));
        }


        var next = await _context.WaitingListEntries
            .Where(w => w.TravelPackageId == packageId && !w.Notified)
            .OrderBy(w => w.CreatedAt).ThenBy(w => w.Id)
            .FirstOrDefaultAsync();

        if (next == null)
        {
            TempData["WaitMsg"] = "Waiting list is empty for this package.";
            return RedirectToAction(nameof(AdminList));
        }

        var subject = $"Room available: {pkg.Destination}";
        var body = $@"
        <h2>Good news!</h2>
        <p>A room is now available for <b>{pkg.Destination}</b> ({pkg.Country}).</p>
        <p>Please log in and complete your booking as soon as possible.</p>
    ";

        await _emailSender.SendAsync(next.Email, subject, body);

        next.Notified = true;
        await _context.SaveChangesAsync();

        TempData["WaitMsg"] = $"Notification sent to: {next.Email}";
        return RedirectToAction(nameof(AdminList));
    }

    private async Task<int> GetPositionAsync(WaitingListEntry entry)
    {
        var aheadCount = await _context.WaitingListEntries.CountAsync(w =>
            w.TravelPackageId == entry.TravelPackageId &&
            !w.Notified &&
            (
                w.CreatedAt < entry.CreatedAt ||
                (w.CreatedAt == entry.CreatedAt && w.Id < entry.Id)
            )
        );

        return aheadCount + 1; 
    }


}
