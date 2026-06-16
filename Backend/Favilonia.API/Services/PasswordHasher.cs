// Re-export from Infrastructure for backward compatibility
using PasswordHasherInfra = Favilonia.Infrastructure.Services.PasswordHasher;

namespace Favilonia.API.Services;

public static class PasswordHasher
{
    public static string Hash(string password) => PasswordHasherInfra.Hash(password);
    public static bool Verify(string password, string passwordHash) => PasswordHasherInfra.Verify(password, passwordHash);
}
