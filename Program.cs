using System.Text;
using System.Reflection;
using Flood_Rescue_Coordination.API.Models;
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

// =============================================
// CẤU HÌNH SWAGGER / OPENAPI
// =============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger Document - API Info
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Flood Rescue Coordination API",
        Description = "API hệ thống điều phối cứu hộ lũ lụt - Quản lý yêu cầu cứu hộ, đội cứu hộ, phân phối hàng cứu trợ",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@floodrescue.vn",
            Url = new Uri("https://floodrescue.vn/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token vào đây.\n\nVí dụ: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Bật XML Comments cho API documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Sắp xếp các endpoints theo tag/controller
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    options.DocInclusionPredicate((name, api) => true);
});

// =============================================
// CẤU HÌNH DATABASE
// =============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// =============================================
// ĐĂNG KÝ SERVICES
// =============================================
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// =============================================
// CẤU HÌNH JWT AUTHENTICATION
// =============================================
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

// =============================================
// CẤU HÌNH AUTHORIZATION
// =============================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("CoordinatorOrAdmin", policy => policy.RequireRole("ADMIN", "COORDINATOR"));
    options.AddPolicy("ManagerOrAbove", policy => policy.RequireRole("ADMIN", "MANAGER", "COORDINATOR"));
    options.AddPolicy("RescueTeam", policy => policy.RequireRole("ADMIN", "COORDINATOR", "RESCUE_TEAM"));
    options.AddPolicy("Citizen", policy => policy.RequireRole("CITIZEN", "ADMIN"));
});

var app = builder.Build();



// =============================================
// CONFIGURE HTTP REQUEST PIPELINE
// =============================================
if (app.Environment.IsDevelopment())
{
    // Swagger JSON endpoint
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "api-docs/{documentName}/swagger.json";
    });

    // Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/api-docs/v1/swagger.json", "Flood Rescue Coordination API v1");
        options.RoutePrefix = "swagger";

        // Cấu hình UI
        options.DocumentTitle = "Flood Rescue API Documentation";
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
        options.ShowExtensions();
        options.EnableValidator();

        // Persist authorization
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

// Thêm middleware kiểm tra token blacklist
app.UseMiddleware<TokenBlacklistMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();