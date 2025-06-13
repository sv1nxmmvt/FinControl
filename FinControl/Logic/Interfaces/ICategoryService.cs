using Server.Data.DTO;
using Server.Data.Models;

namespace Server.Logic.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(Guid userId);
    Task<ServiceResult> CreateCategoryAsync(Guid userId, string name);
}
