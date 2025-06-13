namespace Server.Data.Models;

public class ServiceResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, Error = error };
}
