namespace Identity.Domain.Interfaces;

// El Domain define el CONTRATO. Infrastructure provee la implementación (BCrypt).
// Si mañana queremos cambiar a Argon2, solo tocamos Infrastructure.
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hashedPassword);
}
