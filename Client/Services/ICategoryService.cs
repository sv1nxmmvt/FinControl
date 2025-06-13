using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(Guid userId);
    Task<ServiceResult> CreateCategoryAsync(Guid userId, string name);
}
