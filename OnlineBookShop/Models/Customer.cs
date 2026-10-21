using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineBookShop.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [RegularExpression(@"^[a-zA-Zа-яА-Я]+$", ErrorMessage = "First name must contain only letters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [RegularExpression(@"^[a-zA-Zа-яА-Я]+$", ErrorMessage = "Last name must contain only letters.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = null!;

    public string? Phone { get; set; }
    
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
    public string? PasswordHash { get; set; }

    public string? Role { get; set; } = "User";

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}