using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineBookShop.Models;
using OnlineBookShop.Services;
using Xunit;

namespace OnlineBookShop.Tests
{
    public class BookServiceTests
    {
        private BookWorldDbContext GetDatabaseContext()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<BookWorldDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            var databaseContext = new BookWorldDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task GetAllBooksAsync_ReturnsAllBooksWithCategories()
        {
            var context = GetDatabaseContext();
            var category = new Category { CategoryName = "Fiction" };
            context.Categories.Add(category);
            context.Books.AddRange(
                new Book { Title = "Book 1", Author = "Author 1", Price = 10, StockQuantity = 5, Category = category },
                new Book { Title = "Book 2", Author = "Author 2", Price = 15, StockQuantity = 3, Category = category }
            );
            await context.SaveChangesAsync();

            var service = new BookService(context);
            var result = await service.GetAllBooksAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, b => b.Title == "Book 1");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsBook()
        {
            var context = GetDatabaseContext();
            
            // Създаваме категория, за да работи .Include(b => b.Category) в сервиза
            var category = new Category { CategoryName = "Test Category" };
            context.Categories.Add(category);

            var book = new Book { Title = "Test Book", Author = "Test Author", Price = 20, StockQuantity = 10, Category = category };
            context.Books.Add(book);
            await context.SaveChangesAsync(); // Записваме в базата, за да се генерира BookId

            var service = new BookService(context);
            
            // Търсим по реално генерираното ID на записаната книга
            var result = await service.GetByIdAsync(book.BookId);

            Assert.NotNull(result);
            Assert.Equal("Test Book", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            var context = GetDatabaseContext();
            var service = new BookService(context);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_AddsBookSuccessfully()
        {
            var context = GetDatabaseContext();
            var service = new BookService(context);
            var newBook = new Book { Title = "New Book", Author = "New Author", Price = 25, StockQuantity = 7 };

            await service.CreateAsync(newBook);

            var booksInDb = await context.Books.ToListAsync();
            Assert.Single(booksInDb);
            Assert.Equal("New Book", booksInDb[0].Title);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesBookSuccessfully()
        {
            var context = GetDatabaseContext();
            var book = new Book { Title = "Old Title", Author = "Author", Price = 10, StockQuantity = 5 };
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var service = new BookService(context);

            book.Title = "Updated Title";
            await service.UpdateAsync(book);

            var updatedBook = await context.Books.FindAsync(book.BookId);
            Assert.Equal("Updated Title", updatedBook?.Title);
        }

        [Fact]
        public async Task DeleteAsync_RemovesBookSuccessfully()
        {
            var context = GetDatabaseContext();
            var book = new Book { Title = "Book to Delete", Author = "Author", Price = 10, StockQuantity = 5 };
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var service = new BookService(context);
            await service.DeleteAsync(book.BookId);

            var deletedBook = await context.Books.FindAsync(book.BookId);
            Assert.Null(deletedBook);
        }

        [Fact]
        public async Task GetBooksPagedAsync_FiltersAndPaginesCorrectly()
        {
            var context = GetDatabaseContext();
            var category = new Category { CategoryName = "Science" };
            context.Categories.Add(category);
            context.Books.AddRange(
                new Book { Title = "C# Programming", Author = "John Doe", Price = 30, StockQuantity = 2, Category = category },
                new Book { Title = "Java Programming", Author = "Jane Smith", Price = 35, StockQuantity = 4, Category = category },
                new Book { Title = "Advanced C#", Author = "John Doe", Price = 40, StockQuantity = 1, Category = category }
            );
            await context.SaveChangesAsync();

            var service = new BookService(context);
            var (books, totalPages) = await service.GetBooksPagedAsync("C#", null, 1, 10);

            Assert.Equal(2, books.Count());
            Assert.Equal(1, totalPages);
        }
    }
}