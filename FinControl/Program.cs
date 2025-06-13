using Server.Data;
using Server.Services;
using FinControl.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Server.Logic.Interfaces;
using Server.Logic.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=expense_tracker;Username=postgres;Password=zasada1324";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
