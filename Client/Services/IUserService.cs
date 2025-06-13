using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public interface IUserService
{
    Task<ServiceResult> RegisterAsync(string login, string password);
    Task<LoginResult> LoginAsync(string login, string password);
}
