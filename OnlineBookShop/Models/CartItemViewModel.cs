using System.ComponentModel.DataAnnotations;

namespace OnlineBookShop.Models
{
    public class CartItemViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\-а-яА-ЯёЁ]+$",
            ErrorMessage = "First name can only contain letters, spaces, and hyphens.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\-а-яА-ЯёЁ]+$",
            ErrorMessage = "Last name can only contain letters, spaces, and hyphens.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,15}$", ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cardholder name is required.")]
        [RegularExpression(@"^[a-zA-Z\s\-а-яА-ЯёЁ]+$",
            ErrorMessage = "Cardholder name can only contain letters and spaces.")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card number is required.")]
        [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{4}$|^\d{16}$",
            ErrorMessage = "Card number must be 16 digits (e.g., 1234-5678-9012-3456).")]
        public string CardNumber { get; set; } = string.Empty;

        // НОВИ ПОЛЕТА: Дата на изтичане и CVC код
        [Required(ErrorMessage = "Expiry date is required.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$",
            ErrorMessage = "Expiry date must be in MM/YY format (e.g., 12/28).")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVC is required.")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVC must be 3 or 4 digits.")]
        public string Cvc { get; set; } = string.Empty;

        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal GrandTotal => CartItems.Sum(i => i.TotalPrice);
    }
}