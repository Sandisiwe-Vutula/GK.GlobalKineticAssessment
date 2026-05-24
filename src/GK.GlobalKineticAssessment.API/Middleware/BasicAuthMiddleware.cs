using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GK.GlobalKineticAssessment.API.Configuration;
using Microsoft.Extensions.Options;

namespace GK.GlobalKineticAssessment.API.Middleware;

public sealed class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BasicAuthOptions _opts;
    private readonly ILogger<BasicAuthMiddleware> _logger;

    private static readonly string[] Public = ["/swagger", "/health"];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BasicAuthMiddleware(
        RequestDelegate next,
        IOptions<BasicAuthOptions> opts,
        ILogger<BasicAuthMiddleware> logger)
    { _next = next; _opts = opts.Value; _logger = logger; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (Public.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(ctx); return;
        }

        if (!ctx.Request.Headers.TryGetValue("Authorization", out var header))
        {
            await Challenge(ctx, "Authorization header is missing."); return;
        }

        try
        {
            var parsed = AuthenticationHeaderValue.Parse(header!);
            if (!parsed.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) || parsed.Parameter is null)
            {
                await Challenge(ctx, "Invalid authorization scheme. Use Basic Auth."); return;
            }

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parsed.Parameter));
            var parts = decoded.Split(':', 2);
            if (parts.Length != 2 || parts[0] != _opts.Username || parts[1] != _opts.Password)
            {
                _logger.LogWarning("Failed auth attempt from {IP}", ctx.Connection.RemoteIpAddress);
                await Challenge(ctx, "Invalid username or password."); return;
            }
        }
        catch
        {
            await Challenge(ctx, "Malformed Authorization header."); return;
        }

        await _next(ctx);
    }

    private static async Task Challenge(HttpContext ctx, string message)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            new { message = "Unauthorized. " + message },
            JsonOpts);

        await ctx.Response.WriteAsync(body);
    }
}
