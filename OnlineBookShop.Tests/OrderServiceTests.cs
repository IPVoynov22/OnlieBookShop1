using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public class OrderService
    {
        private readonly BookWorldDbContext _context;

        public OrderService(BookWorldDbContext context)
        {
            _context = context;
        }

        // Взима всички поръчки (за администраторския панел)
        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // Взима поръчка по ID
        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        // Създаване на поръчка с транзакция (поддържа и InMemory за тестове)
        public async Task<bool> CreateOrderAsync(string firstName, string lastName, string email, string phone, List<OrderItem> items)
        {
            bool isInMemory = _context.Database.IsInMemory();
            IDbContextTransaction? transaction = null;

            if (!isInMemory)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                // 1. Проверка на наличностите
                foreach (var item in items)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book == null || book.StockQuantity < item.Quantity)
                    {
                        if (transaction != null) await transaction.RollbackAsync();
                        return false;
                    }
                }

                // 2. Намиране или създаване на клиента в базата
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Phone = phone
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // 3. Създаване на поръчката
                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending",
                    TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 4. Записване на артикулите и намаляване на склада
                foreach (var item in items)
                {
                    item.OrderId = order.OrderId;
                    _context.OrderItems.Add(item);

                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book != null)
                    {
                        book.StockQuantity -= item.Quantity;
                        _context.Books.Update(book);
                    }
                }

                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }
                return true;
            }
            catch (Exception)
            {
                if (transaction != null)
                {
                    try { await transaction.RollbackAsync(); } catch { }
                }
                return false;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        // Администратор одобрява поръчка
        public async Task<bool> ApproveOrderAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.OrderStatus = "Approved";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }

        // Администратор отхвърля поръчка (и връща бройките в склада)
        public async Task<bool> RejectOrderAsync(int orderId)
        {
            bool isInMemory = _context.Database.IsInMemory();
            IDbContextTransaction? transaction = null;

            if (!isInMemory)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.OrderStatus == "Rejected")
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return false;
                }

                foreach (var item in order.OrderItems)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book != null)
                    {
                        book.StockQuantity += item.Quantity;
                        _context.Books.Update(book);
                    }
                }

                order.OrderStatus = "Rejected";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }
                return true;
            }
            catch (Exception)
            {
                if (transaction != null)
                {
                    try { await transaction.RollbackAsync(); } catch { }
                }
                return false;
            }
            finally
            {
                transaction?.Dispose();
            }
        }
    }
}