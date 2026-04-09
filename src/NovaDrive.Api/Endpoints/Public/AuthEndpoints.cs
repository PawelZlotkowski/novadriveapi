// Api/Endpoints/Public/AuthEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using NovaDrive.Infrastructure.Repositories;
using NovaDrive.Domain.Models;
using NovaDrive.Domain.Enums;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/public/auth/sync — called after Auth0 login to sync user to local DB
        group.MapPost("/sync", async (
            HttpContext context,
            IUserRepository userRepo) =>
        {
            var auth0Id = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst("email")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (auth0Id is null || email is null)
                return Results.BadRequest(new { message = "Token must contain sub and email claims" });

            var existingUser = await userRepo.GetByAuth0IdAsync(auth0Id);
            if (existingUser is not null)
            {
                existingUser.LastLoginAt = DateTime.UtcNow;
                await userRepo.UpdateAsync(existingUser);
                return Results.Ok(new { userId = existingUser.Id, role = existingUser.Role.ToString() });
            }

            var newUser = new User
            {
                Email = email,
                Auth0Id = auth0Id,
                Role = UserRole.Passenger,
                LastLoginAt = DateTime.UtcNow
            };

            await userRepo.CreateAsync(newUser);
            return Results.Created($"/api/public/auth/me", new { userId = newUser.Id, role = newUser.Role.ToString() });
        })
        .RequireAuthorization()
        .WithTags("Auth");

        // GET /api/public/auth/me — current user profile
        group.MapGet("/me", async (
            HttpContext context,
            IUserRepository userRepo) =>
        {
            var auth0Id = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (auth0Id is null) return Results.Unauthorized();

            var user = await userRepo.GetByAuth0IdAsync(auth0Id);
            if (user is null) return Results.NotFound(new { message = "User not synced. Call /auth/sync first." });

            return Results.Ok(new
            {
                user.Id,
                user.Email,
                Role = user.Role.ToString(),
                user.LastLoginAt,
                user.CreatedAt
            });
        })
        .RequireAuthorization()
        .WithTags("Auth");

        return group;
    }
}