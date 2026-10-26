using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public interface IBookService
    {
        Task<(IEnumerable<Book> Books, int TotalPages)> GetBooksPagedAsync(string? searchString, string? category, int pageNumber, int pageSize);
        Task<Book?> GetByIdAsync(int id);
        Task CreateAsync(Book book);
        Task UpdateAsync(Book book);
        Task DeleteAsync(int id);
    }
}