using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GimnasioCuerpoSano.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El DNI es obligatorio.")]
            [Display(Name = "DNI")]
            public string DNI { get; set; }

            [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
            [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Debe confirmar la contraseña.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
            public string ConfirmPassword { get; set; }
        }

        // ⭐ MÉTODO HANDLER PARA VALIDACIÓN REMOTA (AJAX) ⭐
        public async Task<IActionResult> OnPostValidateMiembroAsync([FromForm] string dni, [FromForm] string email)
        {
            // Usamos new JsonResult(...) para corregir el error de contexto.

            if (string.IsNullOrEmpty(dni))
            {
                return new JsonResult(true); // Dejar que la validación 'required' del lado del cliente maneje esto
            }

            // 1. Verificar si el DNI existe
            var miembroPorDni = await _context.Miembros
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DNI == dni);

            if (miembroPorDni == null)
            {
                // El DNI NO existe
                return new JsonResult($"El DNI {dni} no está registrado.");
            }

            // 2. Si el DNI existe, verificar la combinación DNI + Email
            if (string.IsNullOrEmpty(email))
            {
                // El DNI es válido, pero falta el email.
                return new JsonResult(true);
            }

            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.DNI == dni && m.Mail == email);

            if (miembro == null)
            {
                // DNI es correcto, pero el Mail no coincide con ese DNI.
                return new JsonResult($"El correo electrónico no coincide con el registrado para el DNI {dni}.");
            }

            // 3. Validar Membresía
            if (miembro.Membresia == null)
            {
                return new JsonResult("El miembro no tiene una membresía asignada.");
            }

            DateTime fechaVencimiento = miembro.FechaAlta.AddMonths(miembro.Membresia.DuracionEnMeses);
            if (fechaVencimiento < DateTime.Now)
            {
                return new JsonResult("Su membresía no está vigente. No puede registrarse.");
            }

            // Si DNI, Email y Membresía son correctos:
            return new JsonResult(true);
        }
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            // =====================================
            // VALIDACIÓN PERSONALIZADA (DNI Y MAIL)
            // =====================================

            // 1.1 Buscar al miembro solo por DNI (para validar si existe)
            var miembroPorDni = await _context.Miembros
                .AsNoTracking() // No necesitamos seguimiento aún
                .FirstOrDefaultAsync(m => m.DNI == Input.DNI);

            if (miembroPorDni == null)
            {
                // El DNI NO existe en la base de datos
                ModelState.AddModelError("Input.DNI", "El DNI ingresado no corresponde a un miembro registrado.");
                return Page();
            }

            // 1.2 Buscar al miembro por DNI y Email (para validar la combinación)
            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.DNI == Input.DNI && m.Mail == Input.Email);

            if (miembro == null)
            {
                // El DNI es correcto (pasó la 1.1), pero el Mail asociado a ese DNI es incorrecto.
                ModelState.AddModelError("Input.Email", $"El correo electrónico no coincide con el registrado para el DNI {Input.DNI}.");
                return Page();
            }


            // =====================================
            // 2. VALIDACIÓN DE MEMBRESÍA
            // =====================================

            if (miembro.Membresia == null)
            {
                ModelState.AddModelError(string.Empty, "El miembro no tiene una membresía asignada.");
                return Page();
            }

            DateTime fechaVencimiento = miembro.FechaAlta.AddMonths(miembro.Membresia.DuracionEnMeses);

            if (fechaVencimiento < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "Su membresía no está vigente. No puede registrarse.");
                return Page();
            }
            // =====================================
            // CREAR USUARIO EN IDENTITY
            // =====================================
            var user = CreateUser();

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario creado correctamente con membresía activa.");

                //Verificar si el rol existe
                var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
                if (!await roleManager.RoleExistsAsync("Miembro"))
                    await roleManager.CreateAsync(new IdentityRole("Miembro"));

                //Asignar el rol “Miembro” al usuario registrado
                await _userManager.AddToRoleAsync(user, "Miembro");

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(Input.Email, "Confirmar cuenta",
                    $"Por favor confirme su cuenta haciendo clic en <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>este enlace</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"No se pudo crear una instancia de '{nameof(IdentityUser)}'.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("El sistema requiere un almacén de usuario con soporte de email.");

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}

