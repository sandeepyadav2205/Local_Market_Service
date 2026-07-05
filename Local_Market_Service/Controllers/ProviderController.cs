using Local_Market_Service.Data;
using Local_Market_Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Local_Market_Service.ViewModels;
namespace Local_Market_Service.Controllers
{
    [Authorize(Roles = "Provider")]
    public class ProviderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProviderController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<Provider?> GetCurrentProviderAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Providers
                .Include(p => p.ApplicationUser)
                .Include(p => p.Services)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // 1. Dashboard (Index)
        public async Task<IActionResult> Index()
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();

            var bookings = await _context.Booking
                .Include(b => b.Customer)
                .ThenInclude(c => c.ApplicationUser)
                .Include(b => b.Service)
                .Where(b => b.ProviderId == provider.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.CompletedBookings = bookings.Count(b => b.Status?.ToLower() == "completed");
            ViewBag.TotalEarnings = bookings.Where(b => b.Status?.ToLower() == "completed").Sum(b => b.Amount ?? 0);
            
            ViewBag.ActiveServices = provider.Services?.Count ?? 0;
            ViewBag.RecentBookings = bookings.Take(5).ToList();

            return View(provider);
        }

        // 2. Profile
        public async Task<IActionResult> Profile()
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();
            return View(provider);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(Provider model, IFormFile? ImageFile)
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();

            // Update user details
            if (provider.ApplicationUser != null)
            {
                provider.ApplicationUser.FullName = model.ApplicationUser?.FullName;
                provider.ApplicationUser.PhoneNumber = model.ApplicationUser?.PhoneNumber;

                if (ImageFile != null)
                {
                    string imageName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    string imagePath = Path.Combine(_env.WebRootPath, "Images", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fs);
                    }
                    
                    // Remove old image
                    if (!string.IsNullOrEmpty(provider.ApplicationUser.profileImage) && provider.ApplicationUser.profileImage != "default.jpg")
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, "Images", provider.ApplicationUser.profileImage);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    provider.ApplicationUser.profileImage = imageName;
                }
            }

            provider.BusinessName = model.BusinessName;
            provider.Experience = model.Experience;
            provider.Address = model.Address;
            provider.City = model.City;
            provider.State = model.State;
            provider.Pincode = model.Pincode;
            provider.Description = model.Description;

            _context.Update(provider);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // 3. Services CRUD
        public async Task<IActionResult> Services()
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();
            var services = await _context.Services
                .Include(s => s.Category)
                .Where(s => s.ProviderId == provider.Id)
                .ToListAsync();
            return View(services);
        }

        public async Task<IActionResult> AddService()
        {
            ViewBag.Categories = await _context.Category.Where(c => c.isActive).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddService(Service model, IFormFile? ImageFile)
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();

            model.ProviderId = provider.Id;
            model.CreatedAt = DateTime.Now;
            model.Status = "Active";

            if (ImageFile != null)
            {
                string imageName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                string imagePath = Path.Combine(_env.WebRootPath, "Images", imageName);
                using (var fs = new FileStream(imagePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fs);
                }
                model.ImageUrl = imageName;
            }

            _context.Services.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Services");
        }

        public async Task<IActionResult> EditService(int id)
        {
            var provider = await GetCurrentProviderAsync();
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.ProviderId == provider.Id);
            if (service == null) return NotFound();

            ViewBag.Categories = await _context.Category.Where(c => c.isActive).ToListAsync();
            return View(service);
        }

        [HttpPost]
        public async Task<IActionResult> EditService(Service model, IFormFile? ImageFile)
        {
            var provider = await GetCurrentProviderAsync();
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == model.Id && s.ProviderId == provider.Id);
            if (service == null) return NotFound();

            service.Name = model.Name;
            service.Description = model.Description;
            service.Price = model.Price;
            service.CategoryId = model.CategoryId;
            service.Status = model.Status ?? "Active";

            if (ImageFile != null)
            {
                if (!string.IsNullOrEmpty(service.ImageUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "Images", service.ImageUrl);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                string imageName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                string imagePath = Path.Combine(_env.WebRootPath, "Images", imageName);
                using (var fs = new FileStream(imagePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fs);
                }
                service.ImageUrl = imageName;
            }

            _context.Update(service);
            await _context.SaveChangesAsync();
            return RedirectToAction("Services");
        }

        public async Task<IActionResult> DeleteService(int id)
        {
            var provider = await GetCurrentProviderAsync();
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.ProviderId == provider.Id);
            if (service != null)
            {
                if (!string.IsNullOrEmpty(service.ImageUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "Images", service.ImageUrl);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Services");
        }

        // 4. Bookings
        public async Task<IActionResult> Bookings()
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();

            var bookings = await _context.Booking
                .Include(b => b.Customer)
                .ThenInclude(c => c.ApplicationUser)
                .Include(b => b.Service)
                .Where(b => b.ProviderId == provider.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> AcceptBooking(int id)
        {
            var provider = await GetCurrentProviderAsync();
            var booking = await _context.Booking.FirstOrDefaultAsync(b => b.Id == id && b.ProviderId == provider.Id);
            if (booking != null)
            {
                booking.Status = "Accepted";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Bookings");
        }
        
        public async Task<IActionResult> CompleteBooking(int id)
        {
            var provider = await GetCurrentProviderAsync();
            var booking = await _context.Booking.FirstOrDefaultAsync(b => b.Id == id && b.ProviderId == provider.Id);
            if (booking != null)
            {
                booking.Status = "Completed";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> RejectBooking(int id)
        {
            var provider = await GetCurrentProviderAsync();
            var booking = await _context.Booking.FirstOrDefaultAsync(b => b.Id == id && b.ProviderId == provider.Id);
            if (booking != null)
            {
                booking.Status = "Rejected";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Bookings");
        }

        // 5. Reviews
        public async Task<IActionResult> Reviews()
        {
            var provider = await GetCurrentProviderAsync();
            if (provider == null) return NotFound();
            
            // Note: Since Review model might not exist, we use Bookings as a placeholder
            // In a real scenario, this would query a Reviews table.
            // For now, we will just return a view.
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Index");
        }
    }
}

