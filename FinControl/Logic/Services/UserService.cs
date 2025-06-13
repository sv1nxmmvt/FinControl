using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Server.Data.Models;
using Server.Logic.Interfaces;

namespace Server.Logic.Services;

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
