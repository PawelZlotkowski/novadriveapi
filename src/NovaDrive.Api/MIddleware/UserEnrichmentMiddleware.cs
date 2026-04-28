// Api/Middleware/UserEnrichmentMiddleware.cs
// Populates app_user_id claim from the local DB so endpoints don't require
// an Auth0 Post-Login Action to inject that claim into the JWT.
namespace NovaDrive.Api.Middleware;

using System.Security.Claims;
using NovaDrive.Infrastructure.Repositories;

public class UserEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public UserEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.User.HasClaim(c => c.Type == "app_user_id"))
        {
            var auth0Id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (auth0Id is not null)
            {
                var user = await userRepository.GetByAuth0IdAsync(auth0Id);
                if (user is not null)
                {
                    var identity = new ClaimsIdentity([new Claim("app_user_id", user.Id.ToString())]);
                    context.User.AddIdentity(identity);
                }
            }
        }

        await _next(context);
    }
}
