using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Interfaces;

// La interfaz vive en Domain. La implementación en Infrastructure.
// Eso es la Dependency Inversion: el Domain define el contrato,
// la infraestructura lo cumple. El Domain no sabe si es Postgres o SQL Server.
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
