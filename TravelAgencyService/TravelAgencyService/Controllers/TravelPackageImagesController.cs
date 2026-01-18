using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyService.Data;
using TravelAgencyService.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System.Text.Json;



namespace TravelAgencyService.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TravelPackageImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;


        public TravelPackageImagesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int travelPackageId, IFormFile file)
        {
            if (travelPackageId <= 0)
                return BadRequest();

            // לוודא שהחבילה קיימת
            var exists = await _context.TravelPackages.AnyAsync(p => p.Id == travelPackageId);
            if (!exists) return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["ImgMsg"] = "Please choose an image file.";
                return RedirectToAction("Edit", "TravelPackages", new { id = travelPackageId });
            }

            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
            {
                TempData["ImgMsg"] = "Image is too large (max 5MB).";
                return RedirectToAction("Edit", "TravelPackages", new { id = travelPackageId });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
            {
                TempData["ImgMsg"] = "Only JPG, PNG, WEBP are allowed.";
                return RedirectToAction("Edit", "TravelPackages", new { id = travelPackageId });
            }

            var folderRel = Path.Combine("uploads", "packages", travelPackageId.ToString());
            var folderAbs = Path.Combine(_env.WebRootPath, folderRel);
            Directory.CreateDirectory(folderAbs);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fileAbs = Path.Combine(folderAbs, fileName);

            using (var stream = new FileStream(fileAbs, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = "/" + Path.Combine(folderRel, fileName).Replace("\\", "/");

            _context.TravelPackageImages.Add(new TravelPackageImage
            {
                TravelPackageId = travelPackageId,
                Url = url
            });

            await _context.SaveChangesAsync();

            TempData["ImgMsg"] = "Image added successfully.";
            return RedirectToAction("Edit", "TravelPackages", new { id = travelPackageId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var img = await _context.TravelPackageImages.FindAsync(id);
            if (img == null) return NotFound();

            var packageId = img.TravelPackageId;

            _context.TravelPackageImages.Remove(img);
            await _context.SaveChangesAsync();

            TempData["ImgMsg"] = "Image deleted.";
            return RedirectToAction("Edit", "TravelPackages", new { id = packageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTemp(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please choose an image file.");

            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
                return BadRequest("Image is too large (max 5MB).");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest("Only JPG, PNG, WEBP are allowed.");

            var folderRel = Path.Combine("uploads", "temp");
            var folderAbs = Path.Combine(_env.WebRootPath, folderRel);
            Directory.CreateDirectory(folderAbs);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fileAbs = Path.Combine(folderAbs, fileName);

            using (var stream = new FileStream(fileAbs, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = "/" + Path.Combine(folderRel, fileName).Replace("\\", "/");

            return Json(new { fileName, url });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTemp(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Missing fileName.");

            var folderAbs = Path.Combine(_env.WebRootPath, "uploads", "temp");
            var fileAbs = Path.Combine(folderAbs, fileName);

            if (System.IO.File.Exists(fileAbs))
                System.IO.File.Delete(fileAbs);

            return Ok();
        }

    }
}
