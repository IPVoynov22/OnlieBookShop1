using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineBookShop.Models;
using OnlineBookShop.Services;

namespace OnlineBookShop.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly IBookService _bookService;
        private readonly BookWorldDbContext _context; // Запазваме го временно заради Dropdown менютата за категориите (SelectList)

        public BooksController(IBookService bookService, BookWorldDbContext context)
        {
            _bookService = bookService;
            _context = context;
        }

        // GET: Books (с поддръжка на Търсене, Категория и Пагинация)
        public async Task<IActionResult> Index(string? searchString, string? category, int pageNumber = 1)
        {
            int pageSize = 8; // Брой книги на страница в нашия бутик

            var (books, totalPages) = await _bookService.GetBooksPagedAsync(searchString, category, pageNumber, pageSize);

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = totalPages;
            ViewData["Categories"] = new SelectList(_context.Categories, "CategoryName", "CategoryName", category);

            return View(books);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Title,Author,Price,StockQuantity,CategoryId")] Book book)
        {
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                await _bookService.CreateAsync(book);
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", book.CategoryId);
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", book.CategoryId);
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("BookId,Title,Author,Price,StockQuantity,CategoryId")] Book book)
        {
            if (id != book.BookId) return NotFound();

            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                try
                {
                    await _bookService.UpdateAsync(book);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _bookService.GetByIdAsync(book.BookId) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", book.CategoryId);
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _bookService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}