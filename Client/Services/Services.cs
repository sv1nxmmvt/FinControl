using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public interface IUserService
{
    Task<ServiceResult> RegisterAsync(string login, string password);
    Task<LoginResult> LoginAsync(string login, string password);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(Guid userId);
    Task<ServiceResult> CreateCategoryAsync(Guid userId, string name);
}

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetExpensesAsync(Guid userId);
    Task<ServiceResult> CreateExpenseAsync(Guid userId, Guid categoryId, decimal amount);
    Task<List<ReportDto>> GetReportAsync(Guid userId);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> RegisterAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return ServiceResult.Fail("Логин и пароль обязательны");

        if (password.Length < 6)
            return ServiceResult.Fail("Пароль должен содержать минимум 6 символов");

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (existingUser != null)
            return ServiceResult.Fail("Пользователь с таким логином уже существует");

        var user = new User
        {
            Login = login,
            PasswordHash = HashPassword(password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<LoginResult> LoginAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return new LoginResult { Success = false, Error = "Логин и пароль обязательны" };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
            return new LoginResult { Success = false, Error = "Неверный логин или пароль" };

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Login),
            new Claim("UserId", user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);

        return new LoginResult { Success = true, Principal = principal };
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}

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