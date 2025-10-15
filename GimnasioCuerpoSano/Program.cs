using GimnasioCuerpoSano.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);



// -----------------------------------------------------
// Servicios principales
// -----------------------------------------------------
builder.Services.AddControllersWithViews();

//Configuraci�n de EF Core (con tu conexi�n actual)
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
// -----------------------------------------------------

//Configuraci�n del pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//Habilitar autenticaci�n y autorizaci�n
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();//esto habilita las páginas de login/register


//Rutas por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// -----------------------------------------------------
// Inicializaci�n de datos (DB + Roles base)
// -----------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Inicializa datos de ejemplo solo si no existen
    DbInitializer.Initialize(context);

    // 👉 Crea los roles si aún no existen
    string[] roles = new[] { "Administrador", "Empleado", "Miembro" };

    foreach (var role in roles)
    {
        var roleExist = await roleManager.RoleExistsAsync(role);
        if (!roleExist)
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
    }



}

// -----------------------------------------------------
await app.RunAsync();



