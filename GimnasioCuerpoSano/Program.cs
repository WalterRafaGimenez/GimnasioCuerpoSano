using System.Globalization;
using Microsoft.AspNetCore.Localization;
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
// Servicios principales y LOCALIZACIÓN (ANTES de app.Build)
// -----------------------------------------------------
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization(); // <-- habilita la localización de DataAnnotations

// **INICIO: CONFIGURACIÓN DE CULTURA**
// 1. Configurar el soporte de Globalización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 2. Configurar la Cultura por defecto
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("es"), // Español genérico
        new CultureInfo("es-AR") // Español de Argentina (ejemplo)
    };

    options.DefaultRequestCulture = new RequestCulture("es"); // Cultura por defecto
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
// **FIN: CONFIGURACIÓN DE CULTURA**

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
// Pipeline HTTP (DESPUÉS de app.Build)
// -----------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// **APLICAR LOCALIZACIÓN (Middleware): DEBE IR AQUÍ**
app.UseRequestLocalization();

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
    else
    {
        // ✅ Nuevo: si existe, aseguramos que tenga el rol
        if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
            await userManager.AddToRoleAsync(adminUser, "Administrador");
    }
}


await app.RunAsync();