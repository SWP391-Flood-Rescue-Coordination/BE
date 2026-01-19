using Flood_Rescue_Coordination.API.Services;

namespace Flood_Rescue_Coordination.API.Middleware;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var token = context.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            var isBlacklisted = await authService.IsTokenBlacklistedAsync(token);
            if (isBlacklisted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = "Token đã bị vô hiệu hóa. Vui lòng đăng nhập lại."
                });
                return;
            }
        }

        await _next(context);
    }
}