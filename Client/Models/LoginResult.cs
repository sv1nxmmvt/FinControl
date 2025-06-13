namespace ExpenseTracker.Models;

public class LoginResult : ServiceResult
{
    public System.Security.Claims.ClaimsPrincipal? Principal { get; set; }
}