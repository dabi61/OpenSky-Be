using System.Security.Cryptography;

namespace BE_OPENSKY.Helpers;

public static class PasswordHelper
{
    /// <summary>
    /// Hash password using BCrypt with work factor 12
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>BCrypt hashed password</returns>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
            
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verify password against BCrypt hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">BCrypt hashed password</param>
    /// <returns>True if password matches hash</returns>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;
            
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception)
        {
            // Invalid hash format or other BCrypt errors
            return false;
        }
    }

    /// <summary>
    /// Check if a hash is a BCrypt hash
    /// </summary>
    /// <param name="hash">Hash to check</param>
    /// <returns>True if hash is BCrypt format</returns>
    public static bool IsBCryptHash(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;
            
        // BCrypt hashes start with $2a$, $2b$, $2x$, or $2y$
        return hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || 
               hash.StartsWith("$2x$") || hash.StartsWith("$2y$");
    }

    /// <summary>
    /// Generate a secure random password
    /// </summary>
    /// <param name="length">Length of password (minimum 8)</param>
    /// <returns>Random password</returns>
    public static string GenerateRandomPassword(int length = 12)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));
            
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*";
        const string allChars = lowerCase + upperCase + digits + specialChars;

        using var rng = RandomNumberGenerator.Create();
        var password = new char[length];
        var bytes = new byte[length * 4];
        rng.GetBytes(bytes);

        // Ensure at least one character from each category
        password[0] = lowerCase[bytes[0] % lowerCase.Length];
        password[1] = upperCase[bytes[1] % upperCase.Length];
        password[2] = digits[bytes[2] % digits.Length];
        password[3] = specialChars[bytes[3] % specialChars.Length];

        // Fill the rest randomly
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[bytes[i] % allChars.Length];
        }

        // Shuffle the password
        for (int i = length - 1; i > 0; i--)
        {
            int j = bytes[i + length] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
