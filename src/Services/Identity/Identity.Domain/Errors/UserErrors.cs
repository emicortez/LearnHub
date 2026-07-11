namespace Identity.Domain.Errors;

// Centralizar los errores del dominio en un lugar.
// Cuando lanzás una excepción, siempre tenés un código de error tipado.
// Facilita el mapeo a HTTP status codes en la API layer.
public static class UserErrors
{
    public static class Email
    {
        public const string Empty = "user.email.empty";
        public const string TooLong = "user.email.too_long";
        public const string InvalidFormat = "user.email.invalid_format";
        public const string AlreadyInUse = "user.email.already_in_use";
    }

    public static class Password
    {
        public const string TooShort = "user.password.too_short";
        public const string TooWeak = "user.password.too_weak";
        public const string Incorrect = "user.password.incorrect";
    }

    public static class General
    {
        public const string NotFound = "user.not_found";
        public const string Inactive = "user.inactive";
    }
}
