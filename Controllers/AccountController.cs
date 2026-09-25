using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryDbContext _context;

        public AccountController(LibraryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Items");
                if (User.IsInRole("Reception")) return RedirectToAction("Index", "Borrowers");
                if (User.IsInRole("Manager")) return RedirectToAction("Index", "Manager");

                return RedirectToAction("Index", "Catalog");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identifier = (model.Username ?? string.Empty).Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || 
                                          u.Email.ToLower() == identifier);

            if (user == null || !user.VerifyPassword(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                model.Password = string.Empty;
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);
            TempData["Success"] = $"Welcome back, {user.FullName} ({user.Role})!";
            return RedirectToRoleDashboard(user.Role, model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been securely signed out.";
            return RedirectToAction("Index", "Catalog");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(UserAccount user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private IActionResult RedirectToRoleDashboard(UserRole role, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return role switch
            {
                UserRole.Admin => RedirectToAction("Index", "Items"),
                UserRole.Reception => RedirectToAction("Index", "Borrowers"),
                UserRole.Manager => RedirectToAction("Index", "Manager"),
                _ => RedirectToAction("Index", "Catalog")
            };
        }
    }
}
