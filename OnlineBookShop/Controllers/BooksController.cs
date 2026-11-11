using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly BookWorldDbContext _context;

        public BooksController(IBookService bookService, BookWorldDbContext context)
        {
            _bookService = bookService;
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> Index(string? searchString, string? category, int pageNumber = 1)
        {
            int pageSize = 8; 

            var (books, totalPages) = await _bookService.GetBooksPagedAsync(searchString, category, pageNumber, pageSize);

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = totalPages;
            ViewData["Categories"] = new SelectList(_context.Categories, "CategoryName", "CategoryName", category);

            return View(books);
        }

        // Експорт към Excel (Само за Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportToExcel()
        {
            var books = await _bookService.GetAllBooksAsync();

            var builder = new StringBuilder();
            builder.AppendLine("ID,Title,Author,Price,Stock,Category");

            foreach (var b in books)
            {
                var categoryName = b.Category?.CategoryName ?? "N/A";
                builder.AppendLine($"{b.BookId},\"{b.Title}\",\"{b.Author}\",{b.Price},{b.StockQuantity},\"{categoryName}\"");
            }

            byte[] fileBytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(fileBytes, "text/csv", $"Books_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Експорт към PDF (Само за Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportToPdf()
        {
            var books = await _bookService.GetAllBooksAsync();
            return View("ReportPdf", books);
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
        public async Task<IActionResult> Create([Bind("Title,Author,Price,StockQuantity,CategoryId")] Book book, IFormFile? imageFile)
        {
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (allowedExtensions.Contains(extension))
                    {
                        var fileName = Guid.NewGuid().ToString() + extension;
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        book.ImageUrl = "/images/books/" + fileName;
                    }
                    else
                    {
                        ModelState.AddModelError("ImageUrl", "Позволени са само файлове с формат .jpg, .jpeg, .png или .webp");
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", book.CategoryId);
                        return View(book);
                    }
                }

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
        public async Task<IActionResult> Edit(int id, [Bind("BookId,Title,Author,Price,StockQuantity,CategoryId,ImageUrl")] Book book, IFormFile? imageFile)
        {
            if (id != book.BookId) return NotFound();

            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        if (allowedExtensions.Contains(extension))
                        {
                            var fileName = Guid.NewGuid().ToString() + extension;
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }

                            if (!string.IsNullOrEmpty(book.ImageUrl))
                            {
                                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                            }

                            book.ImageUrl = "/images/books/" + fileName;
                        }
                    }

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
            var book = await _bookService.GetByIdAsync(id);
            if (book != null)
            {
                if (!string.IsNullOrEmpty(book.ImageUrl))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                await _bookService.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}