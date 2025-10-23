using Microsoft.AspNetCore.Identity;

namespace GimnasioCuerpoSano.Models
{
    // Hereda de IdentityErrorDescriber para sobrescribir los mensajes de error.
    public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
    {
        // Sobrescribe el método que maneja el requisito de caracter no alfanumérico.
        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "La contraseña debe contener al menos un carácter no alfanumérico (símbolo)."
            };
        }

        // (Opcional: Si quieres traducir el requisito de longitud mínima, aunque ya lo tienes en el InputModel)
        /*
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"La contraseña debe tener una longitud mínima de {length} caracteres."
            };
        }
        */
    }
}