using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Salted SHA-256 for save-slot passwords. Stored format is "v1$&lt;salt hex&gt;$&lt;hash hex&gt;".
/// Values without that prefix are legacy plaintext from older saves and are compared as-is,
/// so an old save still opens; the first successful open re-saves it hashed.
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "v1";
    private const string Separator = "$";
    private const int SaltSizeBytes = 16;

    public static bool IsHashed(string stored)
    {
        return !string.IsNullOrEmpty(stored) &&
               stored.StartsWith(Prefix + Separator, StringComparison.Ordinal);
    }

    public static string Hash(string password)
    {
        byte[] salt = new byte[SaltSizeBytes];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);
        return string.Concat(Prefix, Separator, ToHex(salt), Separator, ToHex(Hash(salt, password)));
    }

    public static bool Verify(string password, string stored)
    {
        if (stored == null) return false;
        password ??= string.Empty;

        if (!IsHashed(stored))
            return FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(stored));

        string[] parts = stored.Split(Separator[0]);
        if (parts.Length != 3) return false;
        try
        {
            byte[] salt = FromHex(parts[1]);
            byte[] expected = FromHex(parts[2]);
            return FixedTimeEquals(Hash(salt, password), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Hash(byte[] salt, string password)
    {
        byte[] pwd = Encoding.UTF8.GetBytes(password ?? string.Empty);
        byte[] data = new byte[salt.Length + pwd.Length];
        Buffer.BlockCopy(salt, 0, data, 0, salt.Length);
        Buffer.BlockCopy(pwd, 0, data, salt.Length, pwd.Length);
        using (SHA256 sha = SHA256.Create())
            return sha.ComputeHash(data);
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        int diff = a.Length ^ b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static byte[] FromHex(string hex)
    {
        if (hex.Length % 2 != 0) throw new FormatException("Odd-length hex string.");
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}
