using Server.Data.Models;

namespace Server.Logic.Interfaces;

public interface IUserService
{
    Task<ServiceResult> RegisterAsync(string login, string password);
    Task<LoginResult> LoginAsync(string login, string password);
}
