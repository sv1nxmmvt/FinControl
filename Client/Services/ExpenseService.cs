using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;

    public ExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(Guid userId)
    {
        return await _context.Expenses
            .Where(e => e.UserId == userId)
            .Include(e => e.Category)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                CategoryName = e.Category.Name,
                Amount = e.Amount,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ServiceResult> CreateExpenseAsync(Guid userId, Guid categoryId, decimal amount)
    {
        if (amount <= 0)
            return ServiceResult.Fail("Сумма должна быть больше нуля");

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
            return ServiceResult.Fail("Категория не найдена");

        var expense = new Expense
        {
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            CreatedAt = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<List<ReportDto>> GetReportAsync(Guid userId)
    {
        return await _context.Expenses
            .Where(e => e.UserId == userId)
            .Include(e => e.Category)
            .GroupBy(e => e.Category.Name)
            .Select(g => new ReportDto
            {
                CategoryName = g.Key,
                TotalAmount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToListAsync();
    }
}