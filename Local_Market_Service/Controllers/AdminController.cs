using Local_Market_Service.Data;
using Local_Market_Service.Models;
using Local_Market_Service.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.Json;

namespace Local_Market_Service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly UserManager<ApplicationUser> userManager;
        public AdminController(AppDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.env = env;
            this.userManager = userManager;
        }
        
        public IActionResult Index()
        {
        ViewBag.TotalCustomers = context.Customers.Count();
        ViewBag.ThisMonthCustomer = context.Customers.Count(c => c.CreatedAt.Month == DateTime.Now.Month && c.CreatedAt.Year == DateTime.Now.Year);
        ViewBag.TotalVerifiedProviders = context.Providers.Count(p => p.IsVerified == true);
        ViewBag.TotalPendingProviders = context.Providers.Count(p => p.IsVerified != true); 
        ViewBag.TotalBookings = context.Booking.Count();
            var recentBookings = context.Booking
                .Include(b => b.Service)
                .Include(b => b.Customer).ThenInclude(c => c.ApplicationUser)
                .Where(b => b.BookingDate >= DateTime.Now.AddDays(-1))
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .ToList();
            ViewBag.RecentBookings = recentBookings;

       
        var today = DateTime.Today;
        var revLabels = new List<string>();
        var revValues = new List<decimal>();
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            revLabels.Add(day.ToString("MMM dd"));
            var dayTotal = context.Booking
                .Where(b => b.BookingDate.Date == day)
                .Sum(b => (decimal?)b.Amount) ?? 0m;
            revValues.Add(dayTotal);
        }
        ViewBag.RevLabelsJson = JsonSerializer.Serialize(revLabels);
        ViewBag.RevValuesJson = JsonSerializer.Serialize(revValues);

        
            var catCounts = context.Category
                .Select(cat => new {
                    Name = cat.Name,
                    Count = context.Booking
                        .Where(b => b.Service != null && b.Service.CategoryId == cat.Id)
                        .Count()
                })
                .ToList();
            ViewBag.CatListJson = JsonSerializer.Serialize(catCounts);
            ViewBag.CatCounts = catCounts; 

        
        var provQueue = context.Providers
            .Where(p => p.IsVerified != true)
            .Include(p => p.ApplicationUser)
            .OrderBy(p => p.CreatedAt)
            .Take(5)
            .ToList();
        ViewBag.ProvList = provQueue;

        
        var activities = context.Booking
            .Include(b => b.Customer).ThenInclude(c => c.ApplicationUser)
            .Include(b => b.Service)
            .OrderByDescending(b => b.BookingDate)
            .Take(10)
            .Select(b => new {
                User = b.Customer.ApplicationUser.FullName,
                Text = $"Booked {b.Service.Name}",
                Date = b.BookingDate
            })
            .ToList();
        ViewBag.ActivityFeedJson = JsonSerializer.Serialize(activities);

       
        var payMethods = new List<object>();
        try
        {
          
            payMethods = context.Payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new {
                    Method = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(p => p.Amount)
                })
                .ToList<object>();
        }
        catch (InvalidOperationException)
        {
            
            payMethods = context.Payments
                .AsEnumerable()
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new {
                    Method = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(p => {
                        if (decimal.TryParse(p.Amount?.ToString(), out var v)) return v;
                        return 0m;
                    })
                })
                .ToList<object>();
        }

        ViewBag.PayMethodsJson = JsonSerializer.Serialize(payMethods);
            ViewBag.PayMethods = payMethods;

        return View();
        }

        public IActionResult Analytics()
        {
            var now = DateTime.Now;
            
            var monthlyRevLabels = new List<string>();
            var monthlyRevValues = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                monthlyRevLabels.Add(monthDate.ToString("MMM yyyy"));
                var total = context.Booking
                    .Where(b => b.BookingDate.Year == monthDate.Year && b.BookingDate.Month == monthDate.Month)
                    .Sum(b => (decimal?)b.Amount) ?? 0m;
                monthlyRevValues.Add(total);
            }
            ViewBag.MonthlyRevLabelsJson = JsonSerializer.Serialize(monthlyRevLabels);
            ViewBag.MonthlyRevValuesJson = JsonSerializer.Serialize(monthlyRevValues);

            var customerGrowth = new List<int>();
            var providerGrowth = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                customerGrowth.Add(context.Customers.Count(c => c.CreatedAt.Year == monthDate.Year && c.CreatedAt.Month == monthDate.Month));
                providerGrowth.Add(context.Providers.Count(p => p.CreatedAt.Year == monthDate.Year && p.CreatedAt.Month == monthDate.Month));
            }
            ViewBag.CustomerGrowthJson = JsonSerializer.Serialize(customerGrowth);
            ViewBag.ProviderGrowthJson = JsonSerializer.Serialize(providerGrowth);

            var catCounts = context.Category
                .Select(cat => new {
                    Name = cat.Name,
                    Count = context.Booking.Count(b => b.Service != null && b.Service.CategoryId == cat.Id)
                })
                .Where(c => c.Count > 0)
                .ToList();
            ViewBag.CatPopLabelsJson = JsonSerializer.Serialize(catCounts.Select(c => c.Name));
            ViewBag.CatPopValuesJson = JsonSerializer.Serialize(catCounts.Select(c => c.Count));

            var statusCounts = context.Booking
                .GroupBy(b => string.IsNullOrEmpty(b.Status) ? "Pending" : b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();
            ViewBag.StatusLabelsJson = JsonSerializer.Serialize(statusCounts.Select(s => s.Status));
            ViewBag.StatusValuesJson = JsonSerializer.Serialize(statusCounts.Select(s => s.Count));

            var topProviders = context.Providers
                .Include(p => p.ApplicationUser)
                .Include(p => p.Services)
                .Select(p => new {
                    Id = p.Id,
                    Name = p.ApplicationUser != null ? p.ApplicationUser.FullName : "Unknown",
                    Business = p.BusinessName,
                    Jobs = context.Booking.Count(b => b.ProviderId == p.Id && b.Status == "Completed"),
                    Revenue = context.Booking.Where(b => b.ProviderId == p.Id && b.Status == "Completed").Sum(b => (decimal?)b.Amount) ?? 0m,
                    Rating = p.AverageRating
                })
                .OrderByDescending(p => p.Jobs)
                .Take(5)
                .ToList();
            ViewBag.TopProvidersJson = JsonSerializer.Serialize(topProviders);

            ViewBag.TotalRevenue = context.Booking.Where(b => b.Status == "Completed").Sum(b => (decimal?)b.Amount) ?? 0m;
            ViewBag.TotalBookings = context.Booking.Count();
            ViewBag.AvgBookingValue = ViewBag.TotalBookings > 0 ? ViewBag.TotalRevenue / ViewBag.TotalBookings : 0m;
            ViewBag.TotalUsers = context.Customers.Count() + context.Providers.Count();

            return View();
        }
        public IActionResult Customers()
        {
            var customers = context.Customers.Include(c => c.ApplicationUser).Include(b => b.Bookings).ToList();
            
            ViewBag.TotalCustomers = customers.Count;
            ViewBag.ThisMonthCustomer = customers.Count(c => c.CreatedAt.Month == DateTime.Now.Month && c.CreatedAt.Year == DateTime.Now.Year);

            int activeCustomers = customers
                .Count(c => c.ApplicationUser != null && c.ApplicationUser.IsActive);
            ViewBag.ActiveCustomers = activeCustomers;

            ViewBag.NewSignups = ViewBag.ThisMonthCustomer;

            ViewBag.PendingConfirmation = customers
                .Count(c => c.ApplicationUser != null && !c.ApplicationUser.EmailConfirmed);

            return View(customers);
        }

        public IActionResult AddCustomer()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddCustomer(CustomerAddByAdminVM model)
        {
            if (ModelState.IsValid)
            {
                var appUser = new ApplicationUser()
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    UserName = model.Email,
                    PhoneNumber = model.MobileNumber,
                };

                // Image Upload
                if (model.Image != null)
                {
                    string imageName = Guid.NewGuid().ToString() + "Local_Market_Service" + model.Image.FileName;
                    string imagePath = Path.Combine(env.WebRootPath, "Images", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fs);
                    }
                    appUser.profileImage = imageName;
                }
                var res = await userManager.CreateAsync(appUser, "Sandeep@123");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(appUser, "Customer");
                    var customer = new Customer()
                    {
                        UserId = appUser.Id,
                        Address = model.Address,
                        City = model.City,
                        Pincode = model.Pincode,
                        State = model.State
                    };
                    context.Customers.Add(customer);
                    context.SaveChanges();
                    return RedirectToAction("Customers");
                }

            }
            return View(model);
        }

        public IActionResult EditCustomer(int id)
        {
            var existingCustomer = context.Customers.Include(c => c.ApplicationUser).FirstOrDefault(c => c.Id == id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            var appUser = existingCustomer.ApplicationUser;
            var customer = new CustomerAddByAdminVM()
            {
                FullName = appUser?.FullName,
                Email = appUser?.Email,
                MobileNumber = appUser?.PhoneNumber,
                Address = existingCustomer.Address,
                City = existingCustomer.City,
                State = existingCustomer.State,
                Pincode = existingCustomer.Pincode
            };
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> EditCustomer(CustomerAddByAdminVM model)
        {
            if (ModelState.IsValid)
            {
                var oldCustomer = context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (oldCustomer == null) 
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                oldCustomer.FullName = model.FullName;
                oldCustomer.Email = model.Email;
                oldCustomer.PhoneNumber = model.MobileNumber;

                // Image Upload
                if (model.Image != null)
                {
                    if(!string.IsNullOrEmpty(oldCustomer.profileImage))
                    {
                        var oldImagePath = Path.Combine(env.WebRootPath, "Images", oldCustomer.profileImage);
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    string imageName = Guid.NewGuid().ToString() + "Local_Market_Service" + model.Image.FileName;
                    string imagePath = Path.Combine(env.WebRootPath, "Images", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fs);
                    }
                    oldCustomer.profileImage = imageName;
                }
                var res = await userManager.UpdateAsync(oldCustomer);
                if (res.Succeeded)
                {
                    var customer = context.Customers.FirstOrDefault(c => c.UserId == oldCustomer.Id);
                    if (customer != null)
                    {
                        customer.Address = model.Address;
                        customer.City = model.City;
                        customer.Pincode = model.Pincode;
                        customer.State = model.State;
                        context.Customers.Update(customer);
                        context.SaveChanges();
                    }
                    return RedirectToAction("Customers");
                }
            }
            return View(model);
        }

        public IActionResult DeleteCustomer(string id)
        {
            var user = context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                if(!string.IsNullOrEmpty(user.profileImage))
                {
                    var imagePath = Path.Combine(env.WebRootPath, "Images", user.profileImage);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                var customer = context.Customers.FirstOrDefault(c => c.UserId == user.Id);
                if (customer != null)
                {
                    context.Customers.Remove(customer);
                }
                context.Users.Remove(user);
                context.SaveChanges();
            }
            return RedirectToAction("Customers");
        }
        public IActionResult ViewCustomer(int id)
        {
            var customer = context.Customers
                .Include(c => c.ApplicationUser)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.Service)
                .FirstOrDefault(c => c.Id == id);
                
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // --- PROVIDERS MANAGEMENT ---
        public IActionResult Providers()
        {
            var providers = context.Providers.Include(p => p.ApplicationUser).Include(p => p.Services).ToList();
            return View(providers);
        }

        public IActionResult ViewProvider(int id)
        {
            var provider = context.Providers
                .Include(p => p.ApplicationUser)
                .Include(p => p.ProviderDocument)
                .Include(p => p.Services)
                .FirstOrDefault(p => p.Id == id);
                
            if (provider == null) return NotFound();
            return View(provider);
        }

        public IActionResult ApproveProvider(int id)
        {
            var provider = context.Providers.Include(p => p.ProviderDocument).FirstOrDefault(p => p.Id == id);
            if (provider != null)
            {
                provider.IsVerified = true;
                if (provider.ProviderDocument != null)
                {
                    provider.ProviderDocument.Status = "approved";
                    provider.ProviderDocument.VerifiedAt = DateTime.Now;
                    provider.ProviderDocument.VerifiedBy = User.Identity?.Name ?? "Admin";
                }
                context.Providers.Update(provider);
                context.SaveChanges();
            }
            return RedirectToAction("Providers");
        }

        public IActionResult RejectProvider(int id)
        {
            var provider = context.Providers.Include(p => p.ProviderDocument).FirstOrDefault(p => p.Id == id);
            if (provider != null)
            {
                provider.IsVerified = false;
                if (provider.ProviderDocument != null)
                {
                    provider.ProviderDocument.Status = "rejected";
                    provider.ProviderDocument.VerifiedAt = DateTime.Now;
                    provider.ProviderDocument.VerifiedBy = User.Identity?.Name ?? "Admin";
                }
                context.Providers.Update(provider);
                context.SaveChanges();
            }
            return RedirectToAction("Providers");
        }

        public IActionResult DeleteProvider(int id)
        {
            var provider = context.Providers.Include(p => p.ApplicationUser).FirstOrDefault(p => p.Id == id);
            if (provider != null)
            {
                if (provider.ApplicationUser != null)
                {
                    context.Users.Remove(provider.ApplicationUser);
                }
                context.Providers.Remove(provider);
                context.SaveChanges();
            }
            return RedirectToAction("Providers");
        }

        // --- CATEGORIES MANAGEMENT ---
        public IActionResult Categories()
        {
            var categories = context.Category.ToList();
            return View(categories);
        }

        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(Category model, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    string imageName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    string imagePath = Path.Combine(env.WebRootPath, "Images", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fs);
                    }
                    model.IconUrl = imageName;
                }
                
                context.Category.Add(model);
                context.SaveChanges();
                return RedirectToAction("Categories");
            }
            return View(model);
        }

        public IActionResult EditCategory(int id)
        {
            var category = context.Category.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(Category model, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                var cat = context.Category.FirstOrDefault(c => c.Id == model.Id);
                if (cat != null)
                {
                    cat.Name = model.Name;
                    
                    if (ImageFile != null)
                    {
                        if (!string.IsNullOrEmpty(cat.IconUrl))
                        {
                            var oldIconUrl = Path.Combine(env.WebRootPath, "Images", cat.IconUrl);
                            if (System.IO.File.Exists(oldIconUrl))
                            {
                                System.IO.File.Delete(oldIconUrl);
                            }
                        }
                        
                        string imageName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                        string imagePath = Path.Combine(env.WebRootPath, "Images", imageName);
                        using (var fs = new FileStream(imagePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(fs);
                        }
                        cat.IconUrl = imageName;
                    }
                    
                    context.Category.Update(cat);
                    context.SaveChanges();
                }
                return RedirectToAction("Categories");
            }
            return View(model);
        }

        public IActionResult DeleteCategory(int id)
        {
            var cat = context.Category.FirstOrDefault(c => c.Id == id);
            if (cat != null)
            {
                if (!string.IsNullOrEmpty(cat.IconUrl))
                {
                    var oldIconUrl = Path.Combine(env.WebRootPath, "Images", cat.IconUrl);
                    if (System.IO.File.Exists(oldIconUrl))
                    {
                        System.IO.File.Delete(oldIconUrl);
                    }
                }
                context.Category.Remove(cat);
                context.SaveChanges();
            }
            return RedirectToAction("Categories");
        }

        // --- SERVICES MANAGEMENT ---
        public IActionResult Services()
        {
            var services = context.Services.Include(s => s.Category).Include(s => s.Provider).ThenInclude(p => p.ApplicationUser).ToList();
            return View(services);
        }

        // --- BOOKINGS MANAGEMENT ---
        public IActionResult Bookings()
        {
            var bookings = context.Booking
                .Include(b => b.Customer).ThenInclude(c => c.ApplicationUser)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Service)
                .OrderByDescending(b => b.BookingDate)
                .ToList();
            return View(bookings);
        }

        // --- PAYMENTS MANAGEMENT ---
        public IActionResult Payments()
        {
            var payments = context.Payments.Include(p => p.Booking).OrderByDescending(p => p.PaymentDate).ToList();
            return View(payments);
        }

        // --- NEW PAGES ---
        public IActionResult Reviews()
        {
            var reviews = context.Reviews
                .Include(r => r.Customer).ThenInclude(c => c.ApplicationUser)
                .Include(r => r.Provider).ThenInclude(p => p.ApplicationUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            return View(reviews);
        }

        public IActionResult Documents()
        {
            var providersWithDocs = context.Providers
                .Include(p => p.ApplicationUser)
                .Include(p => p.ProviderDocument)
                .Where(p => p.ProviderDocument != null)
                .OrderByDescending(p => p.ProviderDocument.UploadedAt)
                .ToList();
            return View(providersWithDocs);
        }

        public IActionResult Settings()
        {
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

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Settings");
        }
    }
}
