using Local_Market_Service.Data;
using Local_Market_Service.Models;
using Local_Market_Service.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;

namespace Local_Market_Service.Controllers
{

    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly AppDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly IEmailSender emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AppDbContext context, IWebHostEnvironment env, IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
            this.env = env;
            this.emailSender = emailSender;
        }

        public IActionResult CustomerRegistrationForm() => View();
        [HttpPost]
        public async Task<IActionResult> CustomerRegistrationForm(CustomerRegistrationVM model)
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
                if(model.Image != null)
                {
                    string imageName = Guid.NewGuid().ToString() + "Local_Market_Service" + model.Image.FileName;
                    string imagePath = Path.Combine(env.WebRootPath, "Images", imageName);
                    using(var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fs);
                    }
                    appUser.profileImage = imageName;
                }
                var res = await userManager.CreateAsync(appUser, model.Password);
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
                    return RedirectToAction("Login");
                }
                
            }
            return View(model);
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // Forgot Password
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please provide your email.");
                return View();
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Please provide a valid email.");
                return View();
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            string emailSubject = "Password Reset Request - Local Market Service";
            string emailBody = $@"
                <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #eaeaea; border-radius: 8px; background-color: #ffffff;'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h1 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Local Market Service</h1>
                    </div>
                    <p style='color: #4a5568; font-size: 16px; line-height: 1.5; margin-bottom: 20px;'>Hello,</p>
                    <p style='color: #4a5568; font-size: 16px; line-height: 1.5; margin-bottom: 25px;'>We received a request to reset the password for your Local Market Service account. If you made this request, please click the button below to set a new password.</p>
                    <div style='text-align: center; margin: 35px 0;'>
                        <a href='{callbackUrl}' style='background-color: #3498db; color: #ffffff; padding: 14px 28px; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 6px; display: inline-block; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>Reset Your Password</a>
                    </div>
                    <p style='color: #4a5568; font-size: 14px; line-height: 1.5; margin-bottom: 10px;'>If the button doesn't work, copy and paste the following link into your browser:</p>
                    <p style='word-break: break-all; color: #3498db; font-size: 14px; margin-bottom: 30px;'><a href='{callbackUrl}' style='color: #3498db; text-decoration: underline;'>{callbackUrl}</a></p>
                    <p style='color: #718096; font-size: 14px; line-height: 1.5; margin-bottom: 0;'>If you didn't request this password reset, you can safely ignore this email. Your password will remain unchanged.</p>
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 30px 0;' />
                    <p style='color: #a0aec0; font-size: 12px; text-align: center; margin: 0;'>&copy; {DateTime.Now.Year} Local Market Service. All rights reserved.</p>
                </div>";

            await emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);

            return View("ForgotPasswordConfirmation");
        }

        public IActionResult ResetPassword(string token = null, string email = null)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError("", "Invalid password reset token.");
            }
            return View(new ResetPasswordVM { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        public IActionResult ResetPasswordConfirmation() => View();

        public IActionResult ForgotPasswordConfirmation() => View();

        // Email confirmation
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Index", "Home");

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
                return View("ConfirmEmail");

            return View("Error");
        }

        // External login (Google, etc.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError("", $"Error from external provider: {remoteError}");
                return RedirectToAction(nameof(Login));
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // If the user does not have an account, then create one using info from the external provider.
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

                if (email != null)
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = name,
                            EmailConfirmed = true
                        };
                        var createResult = await userManager.CreateAsync(user);
                        if (createResult.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Customer");
                        }
                    }

                    await userManager.AddLoginAsync(user, info);
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View("ExternalLoginFailure");
        }

        public IActionResult ProviderRegistrationForm()
        {
            ViewBag.Categories = context.Category.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProviderRegistrationForm(ProviderRegistrationVM model)
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

                if (model.Image != null)
                {
                    string imageName = Guid.NewGuid().ToString() + "Local_Market_Service" + model.Image.FileName;
                    string imagePath = Path.Combine(env.WebRootPath, "Images", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fs);
                    }
                    appUser.profileImage= imageName;
                }
                var res = await userManager.CreateAsync(appUser, model.Password);

                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(appUser, "Provider");
                    var provider = new Provider()
                    {
                        UserId = appUser.Id,
                        BusinessName = model.BusinessName,
                        Experience = model.Experience,
                        Description = model.Description,
                        Address = model.Address,
                        City = model.City,
                        State = model.State,
                        Pincode = model.Pincode,
                    };

                    context.Providers.Add(provider);
                    context.SaveChanges();

                    string imageName = Guid.NewGuid().ToString() + "Local_Market_Service" + model.DocumentUrl.FileName;
                    string imagePath = Path.Combine(env.WebRootPath, "Documents", imageName);
                    using (var fs = new FileStream(imagePath, FileMode.Create))
                    {
                        await model.DocumentUrl.CopyToAsync(fs);
                    }

                    var document = new ProviderDocument()
                    {
                        ProviderId = provider.Id,
                        DocumentType = model.DocumentType,
                        DocumentNumber = model.DocumentNumber,
                        DocumentUrl = imageName
                    };

                    context.ProviderDocuments.Add(document);
                    context.SaveChanges();
                    return RedirectToAction("Login");
                }

            }
            ViewBag.Categories = context.Category.ToList();
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = userManager.FindByEmailAsync(model.Email).Result;
                if (user != null)
                {
                    var res = signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false).Result;
                    if (res.Succeeded)
                    {
                        string role = userManager.GetRolesAsync(user).Result.FirstOrDefault()!;
                        if (role == "Admin")
                        {
                            return RedirectToAction("Index", "Admin");

                        }
                        else if (role == "Provider")
                        {
                            return RedirectToAction("Index", "Provider");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Customer");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid Credentials");
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Invalid Credentials");
                }
            }
            return View(model);
        }


    }
}
