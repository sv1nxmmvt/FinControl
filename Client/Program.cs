using ExpenseTracker.Data;
using ExpenseTracker.Services;
using Client.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Настройка подключения к базе данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=expense_tracker;Username=postgres;Password=zasada1324";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Настройка аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Регистрация сервисов
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

// Добавляем HttpContextAccessor для доступа к HttpContext в компонентах
builder.Services.AddHttpContextAccessor();

// Добавление Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Создание базы данных при запуске
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Настройка pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// API endpoint для входа (оставляем как есть, так как не конфликтует)
app.MapGet("/api/login", async (string login, string password, IUserService userService, HttpContext context) =>
{
    var result = await userService.LoginAsync(login, password);
    if (result.Success && result.Principal != null)
    {
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);
        context.Response.Redirect("/expenses");
        return;
    }
    context.Response.Redirect("/?error=" + Uri.EscapeDataString(result.Error ?? "Ошибка входа"));
});

// API endpoints - добавляем префикс /api/ для всех API маршрутов
app.MapPost("/api/register", async (RegisterRequest request, IUserService userService) =>
{
    var result = await userService.RegisterAsync(request.Login, request.Password);
    return result.Success ? Results.Ok() : Results.BadRequest(result.Error);
});

app.MapPost("/api/login-post", async (LoginRequest request, IUserService userService, HttpContext context) =>
{
    var result = await userService.LoginAsync(request.Login, request.Password);
    if (result.Success && result.Principal != null)
    {
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);
        return Results.Ok();
    }
    return Results.BadRequest(result.Error);
});

app.MapPost("/api/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/categories", async (ICategoryService categoryService, HttpContext context) =>
{
    var userId = context.User.FindFirst("UserId")?.Value;
    if (userId == null) return Results.Unauthorized();

    var categories = await categoryService.GetCategoriesAsync(Guid.Parse(userId));
    return Results.Ok(categories);
}).RequireAuthorization();

app.MapPost("/api/categories", async (CreateCategoryRequest request, ICategoryService categoryService, HttpContext context) =>
{
    var userId = context.User.FindFirst("UserId")?.Value;
    if (userId == null) return Results.Unauthorized();

    var result = await categoryService.CreateCategoryAsync(Guid.Parse(userId), request.Name);
    return result.Success ? Results.Ok() : Results.BadRequest(result.Error);
}).RequireAuthorization();

app.MapGet("/api/expenses", async (IExpenseService expenseService, HttpContext context) =>
{
    var userId = context.User.FindFirst("UserId")?.Value;
    if (userId == null) return Results.Unauthorized();

    var expenses = await expenseService.GetExpensesAsync(Guid.Parse(userId));
    return Results.Ok(expenses);
}).RequireAuthorization();

app.MapPost("/api/expenses", async (CreateExpenseRequest request, IExpenseService expenseService, HttpContext context) =>
{
    var userId = context.User.FindFirst("UserId")?.Value;
    if (userId == null) return Results.Unauthorized();

    var result = await expenseService.CreateExpenseAsync(Guid.Parse(userId), request.CategoryId, request.Amount);
    return result.Success ? Results.Ok() : Results.BadRequest(result.Error);
}).RequireAuthorization();

app.MapGet("/api/report", async (IExpenseService expenseService, HttpContext context) =>
{
    var userId = context.User.FindFirst("UserId")?.Value;
    if (userId == null) return Results.Unauthorized();

    var report = await expenseService.GetReportAsync(Guid.Parse(userId));
    return Results.Ok(report);
}).RequireAuthorization();

// Настройка Blazor компонентов - важно добавить это в конце
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public record RegisterRequest(string Login, string Password);
public record LoginRequest(string Login, string Password);
public record CreateCategoryRequest(string Name);
public record CreateExpenseRequest(Guid CategoryId, decimal Amount);