using Server.Data.DTO;
using Server.Data.Models;

namespace Server.Logic.Interfaces;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetExpensesAsync(Guid userId);
    Task<ServiceResult> CreateExpenseAsync(Guid userId, Guid categoryId, decimal amount);
    Task<List<ReportDto>> GetReportAsync(Guid userId);
}
