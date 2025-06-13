namespace ExpenseTracker.Models;

public class ReportDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
