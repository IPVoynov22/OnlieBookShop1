using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineBookShop.Models;

public partial class Book
{
    public int BookId { get; set; }

    [Required(ErrorMessage = "The Book Title is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "The Title must be between 2 and 150 characters.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "The Author name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The Author name must be between 2 and 100 characters.")]
    [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁъѝўў\s\.\-]+$", ErrorMessage = "The Author field can only contain letters, spaces, dots, and hyphens (numbers and special symbols are not allowed).")]
    public string Author { get; set; } = null!;

    [Required(ErrorMessage = "The Price is required.")]
    [Range(0.01, 10000.00, ErrorMessage = "The Price must be a valid number between $0.01 and $10,000.00.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "The Stock Quantity is required.")]
    [Range(0, 100000, ErrorMessage = "The Stock Quantity must be a valid whole number (0 or greater).")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "The Category field is required.")]
    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}