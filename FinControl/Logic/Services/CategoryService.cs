using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using Server.Data.DTO;
using Server.Data.Models;
using Server.Logic.Interfaces;

namespace Server.Logic.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(Guid userId)
    {
        return await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<ServiceResult> CreateCategoryAsync(Guid userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult.Fail("Название категории обязательно");

        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == name);

        if (existingCategory != null)
            return ServiceResult.Fail("Категория с таким названием уже существует");

        var category = new Category
        {
            UserId = userId,
            Name = name.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}
