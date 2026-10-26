using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineBookShop.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "The Category Name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "The Category Name must be between 2 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁъѝўў\s\.\-]+$", ErrorMessage = "The Category Name can only contain letters, spaces, dots, and hyphens (numbers and special symbols are not allowed).")]
    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}