using System.ComponentModel.DataAnnotations;

namespace Server.Data.Models;

public class Category
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public List<Expense> Expenses { get; set; } = new();
}
