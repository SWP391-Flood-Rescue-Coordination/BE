using BCrypt.Net;

namespace HashGenerator;

class Program
{
    static void Main(string[] args)
    {
        var password = args.Length > 0 ? args[0] : "12345";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);

        Console.WriteLine("=========================================");
        Console.WriteLine($"BCrypt Hash Generator for Password: {password}");
        Console.WriteLine("=========================================");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        Console.WriteLine($"Hash Length: {hash.Length}");
        Console.WriteLine();

        // Verify hash
        var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verify Result: {(isValid ? "✓ VALID" : "✗ INVALID")}");
        Console.WriteLine();

        // SQL Update statement for admin
        Console.WriteLine("SQL Update Statement for admin:");
        Console.WriteLine($"UPDATE users SET password_hash = '{hash}' WHERE username = 'admin';");
        Console.WriteLine();

        // SQL Update statement for all users
        Console.WriteLine("SQL Update Statement for ALL users:");
        Console.WriteLine($"UPDATE users SET password_hash = '{hash}' WHERE password_hash = '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';");
        Console.WriteLine();

        // Hash để copy vào database.sql
        Console.WriteLine("Hash để cập nhật vào database.sql:");
        Console.WriteLine(hash);
        Console.WriteLine();

        // Test với hash cũ nếu có
        var oldHash = "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
        Console.WriteLine("Test với hash cũ trong database:");
        var oldHashValid = BCrypt.Net.BCrypt.Verify(password, oldHash);
        Console.WriteLine($"Old Hash Verify Result: {(oldHashValid ? "✓ VALID" : "✗ INVALID")}");
        
        if (!oldHashValid)
        {
            Console.WriteLine();
            Console.WriteLine("⚠️  WARNING: Hash cũ không hợp lệ! Cần cập nhật hash mới vào database.");
        }
    }
}
