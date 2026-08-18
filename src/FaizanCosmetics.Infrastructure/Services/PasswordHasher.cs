using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.Infrastructure.Services;

/// <summary>BCrypt-based password hashing. Work factor 12 balances login latency against brute-force resistance for a desktop LOB app.</summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword)
    {
        if (string.IsNullOrEmpty(plainTextPassword))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(plainTextPassword));
        }
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: WorkFactor);
    }

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        if (string.IsNullOrEmpty(plainTextPassword) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Stored hash is malformed/unrecognized — treat as a failed verification, not a crash.
            return false;
        }
    }
}
