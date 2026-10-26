using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OnlineBookShop.Models;

namespace OnlineBookShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly BookWorldDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(BookWorldDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login (Генерира JWT токен)
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

            // Генериране на JWT Token с претенции (Claims)
            var token = GenerateJwtToken(customer);

            // Запазване на токена в Header / Response за клиента
            Response.Headers.Append("Authorization", "Bearer " + token);

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
                    var existingUser = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("", "A user with this email already exists.");
                        return View(model);
                    }

                    model.Role = "User";

                    _context.Customers.Add(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Login", "Account");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "A database error occurred during registration. Please check your input data.");
                    return View(model);
                }
            }
            return View(model);
        }

        // Logout
        public IActionResult Logout()
        {
            Response.Headers.Remove("Authorization");
            return RedirectToAction("Index", "Home");
        }

        // Помощен метод за генериране на JWT Token
        private string GenerateJwtToken(Customer customer)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, customer.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, customer.Email),
                new Claim(ClaimTypes.Role, customer.Role ?? "User"),
                new Claim("CustomerId", customer.CustomerId.ToString()),
                new Claim("FullName", $"{customer.FirstName} {customer.LastName}")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}