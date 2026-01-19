using System.Text;
using Flood_Rescue_Coordination.API.Data;
using Flood_Rescue_Coordination.API.Middleware;
using Flood_Rescue_Coordination.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Đảm bảo load configuration từ appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();

// Cấu hình Swagger với JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Flood Rescue Coordination API", 
        Version = "v1" 
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Cấu hình DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Đăng ký Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// Cấu hình Authorization với Role-based policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("CoordinatorOrAdmin", policy => policy.RequireRole("ADMIN", "COORDINATOR"));
    options.AddPolicy("ManagerOrAbove", policy => policy.RequireRole("ADMIN", "MANAGER", "COORDINATOR"));
    options.AddPolicy("RescueTeam", policy => policy.RequireRole("ADMIN", "COORDINATOR", "RESCUE_TEAM"));
    options.AddPolicy("Citizen", policy => policy.RequireRole("CITIZEN", "ADMIN"));
});

var app = builder.Build();

// Tự động tạo các bảng còn thiếu
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Tạo bảng refresh_tokens nếu chưa tồn tại
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refresh_tokens')
            BEGIN
                CREATE TABLE refresh_tokens (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    user_id INT NOT NULL,
                    token NVARCHAR(500) NOT NULL,
                    expires_at DATETIME2 NOT NULL,
                    created_at DATETIME2 DEFAULT GETDATE(),
                    revoked_at DATETIME2 NULL,
                    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (user_id) REFERENCES users(user_id)
                );
                CREATE INDEX IX_RefreshTokens_Token ON refresh_tokens(token);
                CREATE INDEX IX_RefreshTokens_UserId ON refresh_tokens(user_id);
            END
        ");

        // Tạo bảng blacklisted_tokens nếu chưa tồn tại
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'blacklisted_tokens')
            BEGIN
                CREATE TABLE blacklisted_tokens (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    token NVARCHAR(1000) NOT NULL,
                    blacklisted_at DATETIME2 DEFAULT GETDATE(),
                    expires_at DATETIME2 NOT NULL
                );
                CREATE INDEX IX_BlacklistedTokens_Token ON blacklisted_tokens(token);
                CREATE INDEX IX_BlacklistedTokens_ExpiresAt ON blacklisted_tokens(expires_at);
            END
        ");

        Console.WriteLine("Database tables verified/created successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating tables: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Thêm middleware kiểm tra token blacklist
app.UseMiddleware<TokenBlacklistMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();