using Local_Market_Service.Data;
using Local_Market_Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Local_Market_Service.ViewModels;
using RazorpayClient = Razorpay.Api.RazorpayClient;
using RazorpayOrder = Razorpay.Api.Order;
namespace Local_Market_Service.Controllers
{
     [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration config;

        public CustomerController(IConfiguration config, AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            this.config = config;
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Customers
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
        }

        // 1. Dashboard
        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var recentBookings = await _context.Booking
                .Include(b => b.Service)
                .Include(b => b.Provider)
                .ThenInclude(p => p.ApplicationUser)
                .Where(b => b.CustomerId == customer.Id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(3)
                .ToListAsync();

            var categories = await _context.Category.Where(c => c.isActive).Take(6).ToListAsync();
            var popularServices = await _context.Services
                .Include(s => s.Provider)
                .ThenInclude(p => p.ApplicationUser)
                .Where(s => s.Status == "Active")
                .OrderByDescending(s => s.Id) 
                .Take(6)
                .ToListAsync();

            ViewBag.RecentBookings = recentBookings;
            ViewBag.Categories = categories;
            ViewBag.PopularServices = popularServices;
            ViewBag.CartCount = await _context.Carts.CountAsync(c => c.CustomerId == customer.Id);

            return View(customer);
        }

        
        public async Task<IActionResult> Explore(int? categoryId, string? search)
        {
            var query = _context.Services
                .Include(s => s.Provider)
                .ThenInclude(p => p.ApplicationUser)
                .Include(s => s.Category)
                .Where(s => s.Status == "Active");

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId.Value);
                ViewBag.SelectedCategory = categoryId.Value;
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.Name.Contains(search) || s.Description.Contains(search));
                ViewBag.SearchTerm = search;
            }

            var services = await query.ToListAsync();
            ViewBag.Categories = await _context.Category.Where(c => c.isActive).ToListAsync();
            
            var customer = await GetCurrentCustomerAsync();
            ViewBag.CartCount = customer != null ? await _context.Carts.CountAsync(c => c.CustomerId == customer.Id) : 0;

            return View(services);
        }

        
        [HttpPost]
        public async Task<IActionResult> AddToCart(int serviceId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return Unauthorized();

            var existingItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.ServiceId == serviceId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                _context.Carts.Update(existingItem);
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    CustomerId = customer.Id,
                    ServiceId = serviceId,
                    Quantity = 1,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Explore");
        }

        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return Unauthorized();

            var cartItem = await _context.Carts.FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customer.Id);
            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Cart");
        }

        public async Task<IActionResult> Cart()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var cartItems = await _context.Carts
                .Include(c => c.Service)
                .ThenInclude(s => s.Provider)
                .ThenInclude(p => p.ApplicationUser)
                .Where(c => c.CustomerId == customer.Id)
                .ToListAsync();

            ViewBag.CartCount = cartItems.Count;
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> BookNow(int serviceId, string PaymentMethod = "Cash")
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();

            if (PaymentMethod == "Online")
            {
                try 
                {
                    string key = config["Razorpay:Key"]!;
                    string secret = config["Razorpay:Secret"]!;
                    
                    RazorpayClient client = new RazorpayClient(key, secret);
                    Dictionary<string, object> options = new Dictionary<string, object>();
                    
                    options.Add("amount", (int)(service.Price * 100)!); 
                    options.Add("currency", "INR");
                    options.Add("receipt", "rcpt_" + Guid.NewGuid().ToString().Substring(0, 8));
                    
                    RazorpayOrder order = client.Order.Create(options);
                    
                    ViewBag.OrderId = order["id"].ToString();
                    ViewBag.Key = key;
                    ViewBag.Amount = (int)(service.Price * 100)!;
                    ViewBag.ServiceId = service.Id;
                    
                    return View("RazorpayCheckout");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Payment gateway initialization failed. Please pay by Cash.";
                    return RedirectToAction("Bookings");
                }
            }
            else 
            {
                var booking = new Booking
                {
                    CustomerId = customer.Id,
                    ProviderId = service.ProviderId,
                    ServiceId = service.Id,
                    BookingDate = DateTime.Now,
                    ServiceDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                    Address = string.IsNullOrWhiteSpace(customer.Address) ? "Default Address" : customer.Address,
                    Amount = (decimal?)service.Price,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Booking.Add(booking);
                
                var payment = new Payment
                {
                    Booking = booking,
                    Amount = service.Price,
                    PaymentMethod = PaymentMethod,
                    PaymentDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Pending"
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Your service has been booked successfully!";
                return RedirectToAction("Bookings");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PaymentSuccess(string razorpay_payment_id, string razorpay_order_id, string razorpay_signature, int serviceId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();

            string secret = config["Razorpay:Secret"]!;
            string payload = $"{razorpay_order_id}|{razorpay_payment_id}";
            string expectedSignature;
            
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            if (expectedSignature != razorpay_signature)
            {
                TempData["Error"] = "Payment signature verification failed. Booking aborted.";
                return RedirectToAction("Bookings");
            }

            var booking = new Booking
            {
                CustomerId = customer.Id,
                ProviderId = service.ProviderId,
                ServiceId = service.Id,
                BookingDate = DateTime.Now,
                ServiceDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                Address = string.IsNullOrWhiteSpace(customer.Address) ? "Default Address" : customer.Address,
                Amount = (decimal?)service.Price,
                Status = "Confirmed",
                CreatedAt = DateTime.Now
            };

            _context.Booking.Add(booking);
            
            var payment = new Payment
            {
                Booking = booking,
                Amount = service.Price,
                PaymentMethod = "Online",
                PaymentDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Paid"
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment successful! Your booking is confirmed.";
            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> Checkout()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var cartItems = await _context.Carts
                .Include(c => c.Service)
                .Where(c => c.CustomerId == customer.Id)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Cart");

            ViewBag.CartCount = cartItems.Count;
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(DateTime ServiceDate, string Address, string? Remark, string PaymentMethod = "Cash")
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var cartItems = await _context.Carts
                .Include(c => c.Service)
                .Where(c => c.CustomerId == customer.Id)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Cart");

            var bookings = new List<Booking>();
            var payments = new List<Payment>();

            foreach (var item in cartItems)
            {
                var booking = new Booking
                {
                    CustomerId = customer.Id,
                    ProviderId = item.Service.ProviderId,
                    ServiceId = item.ServiceId,
                    BookingDate = DateTime.Now,
                    ServiceDate = DateOnly.FromDateTime(ServiceDate),
                    Address = Address,
                    Amount = (decimal?)item.Service.Price,
                    Status = "Pending",
                    Remark = Remark,
                    CreatedAt = DateTime.Now
                };
                bookings.Add(booking);

                payments.Add(new Payment
                {
                    Booking = booking,
                    Amount = item.Service.Price,
                    PaymentMethod = PaymentMethod,
                    PaymentDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Pending"
                });
            }

            _context.Booking.AddRange(bookings);
            _context.Payments.AddRange(payments);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your bookings have been placed successfully!";
            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> Bookings()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var bookings = await _context.Booking
                .Include(b => b.Service)
                .Include(b => b.Provider)
                .ThenInclude(p => p.ApplicationUser)
                .Where(b => b.CustomerId == customer.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.CartCount = await _context.Carts.CountAsync(c => c.CustomerId == customer.Id);
            return View(bookings);
        }

        public async Task<IActionResult> CancelBooking(int id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            var booking = await _context.Booking.FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customer.Id);
            if (booking != null && booking.Status?.ToLower() == "pending")
            {
                booking.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> Profile()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            ViewBag.CartCount = await _context.Carts.CountAsync(c => c.CustomerId == customer.Id);
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(Customer model, IFormFile? ImageFile)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return NotFound();

            if (customer.ApplicationUser != null)
            {
                customer.ApplicationUser.FullName = model.ApplicationUser?.FullName;
                customer.ApplicationUser.PhoneNumber = model.ApplicationUser?.PhoneNumber;

                if (ImageFile != null)
                {
                    string imageName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    string imagePath = Path.Combine(_env.WebRootPath, "Images", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fs);
                    }
                    
                    if (!string.IsNullOrEmpty(customer.ApplicationUser.profileImage) && customer.ApplicationUser.profileImage != "default.jpg")
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, "Images", customer.ApplicationUser.profileImage);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    customer.ApplicationUser.profileImage = imageName;
                }
            }

            customer.Address = model.Address;
            customer.City = model.City;
            customer.State = model.State;
            customer.Pincode = model.Pincode;

            _context.Update(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
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
            return RedirectToAction("Profile");
        }
    }
}

