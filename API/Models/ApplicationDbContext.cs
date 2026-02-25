using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Flood_Rescue_Coordination.API.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RescueRequest> RescueRequests { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<VehicleType> VehicleTypes { get; set; }
    public DbSet<RescueTeam> RescueTeams { get; set; }
    public DbSet<RescueOperation> RescueOperations { get; set; }
    public DbSet<RescueOperationVehicle> RescueOperationVehicles { get; set; }
    public DbSet<RescueRequestStatusHistory> RescueRequestStatusHistories { get; set; }
    public DbSet<RescueTeamMember> RescueTeamMembers { get; set; }

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
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(300);
        });

        modelBuilder.Entity<RescueRequest>(entity =>
        {
            entity.ToTable("rescue_requests");
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.CitizenId).HasColumnName("citizen_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(300);
            entity.Property(e => e.PriorityLevelId).HasColumnName("priority_level_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(e => e.Citizen)
                  .WithMany()
                  .HasForeignKey(e => e.CitizenId);

            entity.HasOne(e => e.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedBy);
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.ToTable("vehicle_types");
            entity.HasKey(e => e.VehicleTypeId);
            entity.Property(e => e.VehicleTypeId).HasColumnName("vehicle_type_id");
            entity.Property(e => e.TypeCode).HasColumnName("type_code").HasMaxLength(50);
            entity.Property(e => e.TypeName).HasColumnName("type_name").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasKey(e => e.VehicleId);
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.VehicleCode).HasColumnName("vehicle_code").HasMaxLength(20);
            entity.Property(e => e.VehicleName).HasColumnName("vehicle_name").HasMaxLength(100);
            entity.Property(e => e.VehicleTypeId).HasColumnName("vehicle_type_id");
            entity.Property(e => e.LicensePlate).HasColumnName("license_plate").HasMaxLength(20);
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.CurrentLocation).HasColumnName("current_location").HasMaxLength(300);
            entity.Property(e => e.LastMaintenance).HasColumnName("last_maintenance");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.VehicleType)
                  .WithMany()
                  .HasForeignKey(e => e.VehicleTypeId);
        });

        modelBuilder.Entity<RescueTeam>(entity =>
        {
            entity.ToTable("rescue_teams");
            entity.HasKey(e => e.TeamId);
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.TeamName).HasColumnName("team_name").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<RescueOperation>(entity =>
        {
            entity.ToTable("rescue_operations");
            entity.HasKey(e => e.OperationId);
            entity.Property(e => e.OperationId).HasColumnName("operation_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        });

        modelBuilder.Entity<RescueOperationVehicle>(entity =>
        {
            entity.ToTable("rescue_operation_vehicles");
            entity.HasKey(e => new { e.OperationId, e.VehicleId });
            entity.Property(e => e.OperationId).HasColumnName("operation_id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
        });

        modelBuilder.Entity<RescueRequestStatusHistory>(entity =>
        {
            entity.ToTable("rescue_request_status_history");
            entity.HasKey(e => e.StatusId);
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<RescueTeamMember>(entity =>
        {
            entity.ToTable("rescue_team_members");
            entity.HasKey(e => new { e.TeamId, e.UserId });
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.MemberRole).HasColumnName("member_role").HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
            entity.Property(e => e.LeftAt).HasColumnName("left_at");
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

    private void AutoHashPasswords()
    {
        var users = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();

        foreach (var user in users)
        {
            if (!string.IsNullOrEmpty(user.PasswordHash) && 
                !user.PasswordHash.StartsWith("$2a$") && 
                !user.PasswordHash.StartsWith("$2b$") && 
                !user.PasswordHash.StartsWith("$2y$"))
            {
                if (user.PasswordHash.Length < 60)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }
            }
        }
    }
}