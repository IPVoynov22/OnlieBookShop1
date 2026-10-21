using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBookShop.Models;
using System.Security.Claims;

namespace OnlineBookShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly BookWorldDbContext _context;

        public AccountController(BookWorldDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email && c.PasswordHash == password);

            if (customer == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, customer.Email),
                new Claim(ClaimTypes.Role, customer.Role ?? "User"),
                new Claim("FullName", $"{customer.FirstName} {customer.LastName}")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists in the database
                    var existingUser = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("", "A user with this email already exists.");
                        return View(model);
                    }

                    // Set default role for new registrations
                    model.Role = "User"; 

                    _context.Customers.Add(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Login", "Account");
                }
                catch (DbUpdateException)
                {
                    // Catch database constraint conflicts gracefully
                    ModelState.AddModelError("", "A database error occurred during registration. Please check your input data.");
                    return View(model);
                }
            }
            return View(model);
        }

        // Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}