using System.Globalization;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------
// Configurar licencia de QuestPDF (Community)
// -----------------------------------------------------
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// -----------------------------------------------------
// Logging detallado para debug
// -----------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = false;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// -----------------------------------------------------
// Servicios principales
// -----------------------------------------------------
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization(); // <-- habilita la localización de DataAnnotations

// Configuración de EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Agregamos Identity (usuarios y roles)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddErrorDescriber<SpanishIdentityErrorDescriber>() // <-- tu clase de errores en español
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// -----------------------------------------------------
// Configuración de la cultura por defecto
// -----------------------------------------------------
var supportedCultures = new[] { new CultureInfo("es-AR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("es-AR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// -----------------------------------------------------
// Pipeline HTTP
// -----------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages(); // Login/Register

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// -----------------------------------------------------
// Inicialización de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    DbInitializer.Initialize(context);

    string[] roles = new[] { "Administrador", "Empleado", "Miembro", "Entrenador" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    string adminEmail = "admin@gimnasio.com";
    string adminPass = "Admin123$";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var newAdmin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(newAdmin, adminPass);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(newAdmin, "Administrador");
        else
            foreach (var error in result.Errors)
                Console.WriteLine($"Error creando admin: {error.Description}");
    }
}

await app.RunAsync();
