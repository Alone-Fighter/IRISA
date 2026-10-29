using IRISA.Backend.Data;
using IRISA.Backend.Models;
using IRISA.Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var applicationConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");
var sourceConnectionString = builder.Configuration.GetConnectionString("SourceConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'SourceConnection' is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(applicationConnectionString));
builder.Services.AddDbContext<SourceDataContext>(options =>
    options.UseSqlServer(
        sourceConnectionString,
        sql => sql.MigrationsHistoryTable("__SourceMigrationsHistory")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "IRISA.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    var origins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>()
        ?? ["http://localhost:5173"];
    policy.WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

builder.Services.AddScoped<IPersonLookupService, PersonLookupService>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var applicationDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var sourceDb = scope.ServiceProvider.GetRequiredService<SourceDataContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Applying application database migrations.");
    await applicationDb.Database.MigrateAsync();
    logger.LogInformation("Application database migrations completed.");

    if (app.Configuration.GetValue<bool>("DemoData:InitializeSourceDatabase"))
    {
        logger.LogInformation("Applying local demo source database migrations.");
        await sourceDb.Database.MigrateAsync();
        logger.LogInformation("Local demo source database migrations completed.");

        if (app.Configuration.GetValue<bool>("DemoData:SeedSourceDatabase"))
        {
            await DemoDataSeeder.SeedSourceAsync(sourceDb);
        }
    }

    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DemoDataSeeder.SeedDevelopmentUserAsync(
            userManager,
            app.Configuration,
            logger);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapOpenApi().AllowAnonymous();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
