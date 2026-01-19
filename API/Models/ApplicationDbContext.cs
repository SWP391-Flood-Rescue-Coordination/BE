using Microsoft.EntityFrameworkCore;
using Flood_Rescue_Coordination.API.Models;
using BCrypt.Net;

namespace Flood_Rescue_Coordination.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(500);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<BlacklistedToken>(entity =>
        {
            entity.ToTable("blacklisted_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(1000);
            entity.Property(e => e.BlacklistedAt).HasColumnName("blacklisted_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            
            entity.HasIndex(e => e.Token);
        });
    }

    public override int SaveChanges()
    {
        AutoHashPasswords();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AutoHashPasswords();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tự động hash password cho các user mới hoặc user có password chưa được hash
    /// </summary>
    private void AutoHashPasswords()
    {
        var users = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();

        foreach (var user in users)
        {
            // Kiểm tra nếu password_hash không phải là BCrypt hash format (bắt đầu với $2a$, $2b$, hoặc $2y$)
            if (!string.IsNullOrEmpty(user.PasswordHash) && 
                !user.PasswordHash.StartsWith("$2a$") && 
                !user.PasswordHash.StartsWith("$2b$") && 
                !user.PasswordHash.StartsWith("$2y$"))
            {
                // Nếu password_hash trông giống plain text password, hash nó
                // Lưu ý: Chỉ hash nếu độ dài < 60 (BCrypt hash thường dài 60 ký tự)
                if (user.PasswordHash.Length < 60)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }
            }
        }
    }
}