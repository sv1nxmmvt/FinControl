using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public List<Category> Categories { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
}
