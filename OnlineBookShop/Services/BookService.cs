using Microsoft.EntityFrameworkCore;
using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public class BookService : IBookService
    {
        private readonly BookWorldDbContext _context;

        public BookService(BookWorldDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Book> Books, int TotalPages)> GetBooksPagedAsync(string? searchString, string? category, int pageNumber, int pageSize)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .AsQueryable();

            // Филтриране по категория (ако е подадена)
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(b => b.Category.CategoryName == category);
            }

            // Търсене по заглавие или автор
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Title.Contains(searchString) || b.Author.Contains(searchString));
            }

            // Общ брой записи за изчисляване на страниците
            int totalBooks = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            // Пагинация
            var books = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (books, totalPages);
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.BookId == id);
        }

        public async Task CreateAsync(Book book)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Book book)
        {
            _context.Update(book);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
        }
    }
}