using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBookShop.Models;
using OnlineBookShop.Services;

namespace OnlineBookShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly OrderService _orderService;
        private readonly BookWorldDbContext _context;
        private const string CartSessionKey = "CartSession";

        public OrdersController(OrderService orderService, BookWorldDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        // Помощен метод за четене на количката от Session
        private List<CartItemViewModel> GetCart()
        {
            var sessionData = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(sessionData))
            {
                return new List<CartItemViewModel>();
            }
            return JsonSerializer.Deserialize<List<CartItemViewModel>>(sessionData) ?? new List<CartItemViewModel>();
        }

        // Помощен метод за записване на количката в Session
        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        //  Преглед на количката
        public IActionResult Cart()
        {
            var cart = GetCart();
            return View(cart);
        }

        //  Добавяне на книга в количката
        [HttpPost]
        public async Task<IActionResult> AddToCart(int bookId, int quantity = 1)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null || book.StockQuantity < quantity)
            {
                TempData["Error"] = "Book is out of stock or not found.";
                return RedirectToAction("Index", "Home");
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(i => i.BookId == bookId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Price = book.Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return RedirectToAction("Cart");
        }

        //  Премахване на артикул от количката
        public IActionResult RemoveFromCart(int bookId)
        {
            var cart = GetCart();
            cart.RemoveAll(i => i.BookId == bookId);
            SaveCart(cart);
            return RedirectToAction("Cart");
        }

        // GET: Checkout (Форма за попълване на данни и плащане)
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart
            };

            return View(model);
        }

        //  POST: Checkout (Финализиране и създаване на поръчка)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Cart");
            }

            model.CartItems = cart;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Преобразуваме артикулите от количката към OrderItem модели за сървиса
            var orderItems = cart.Select(i => new OrderItem
            {
                BookId = i.BookId,
                Quantity = i.Quantity,
                UnitPrice = i.Price
            }).ToList();

            // Извикваме сървиса за създаване на поръчката и транзакцията
            var success = await _orderService.CreateOrderAsync(
                model.FirstName, 
                model.LastName, 
                model.Email, 
                model.Phone, 
                orderItems
            );

            if (!success)
            {
                ModelState.AddModelError("", "Order could not be created. Some books might be out of stock.");
                return View(model);
            }

            // Изчистваме количката при успешна поръчка
            HttpContext.Session.Remove(CartSessionKey);

            TempData["Success"] = "Your order was successfully placed and is pending approval!";
            return RedirectToAction("OrderConfirmation");
        }

        //  Страница за успешна поръчка
        public IActionResult OrderConfirmation()
        {
            return View();
        }

        // ==========================================
        // АДМИНИСТРАТОРСКИ ПАНЕЛ ЗА ПОРЪЧКИ
        // ==========================================

        //  Списък с всички поръчки за админа
        [Authorize] // Само за оторизирани потребители (администратори)
        public async Task<IActionResult> AdminOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        //  Одобряване на поръчка
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Approve(int id)
        {
            await _orderService.ApproveOrderAsync(id);
            return RedirectToAction("AdminOrders");
        }

        //  Отхвърляне на поръчка (и възстановяване на склада)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Reject(int id)
        {
            await _orderService.RejectOrderAsync(id);
            return RedirectToAction("AdminOrders");
        }
    }
}