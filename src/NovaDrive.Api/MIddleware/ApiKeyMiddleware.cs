// Api/Middleware/ApiKeyMiddleware.cs
namespace NovaDrive.Api.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check API key for /api/vehicle/* routes
        if (!context.Request.Path.StartsWithSegments("/api/vehicle"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "API key is missing" });
            return;
        }

        var validApiKey = _configuration["ApiKeys:VehicleSystem"];
        if (!string.Equals(extractedApiKey, validApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid API key" });
            return;
        }

        await _next(context);
    }
}