namespace Server.Data.DTO;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
