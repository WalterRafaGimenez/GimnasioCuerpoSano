using GimnasioCuerpoSano.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------
// Logging detallado para debug
// -----------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// -----------------------------------------------------
// Servicios principales
// -----------------------------------------------------
builder.Services.AddControllersWithViews();

// Configuración de EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Agregamos Identity (usuarios y roles)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// -----------------------------------------------------
var app = builder.Build();

// Configuración del pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Habilitar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages(); // Login/Register

// Rutas por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// -----------------------------------------------------
// Inicialización de datos (DB + Roles base)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Inicializa datos de ejemplo solo si no existen
    DbInitializer.Initialize(context);

    // Crear roles base
    string[] roles = new[] { "Administrador", "Empleado", "Miembro" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Crear usuario administrador inicial si no existe
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
        {
            await userManager.AddToRoleAsync(newAdmin, "Administrador");
        }
        else
        {
            // Loguea errores si no se pudo crear el admin
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error creando admin: {error.Description}");
            }
        }
    }
}

// -----------------------------------------------------
await app.RunAsync();


