// Api/Extensions/ClaimsPrincipalExtensions.cs
namespace NovaDrive.Api.Extensions;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static string GetAuth0Id(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("app_user_id");
        return claim is not null ? Guid.Parse(claim) : throw new UnauthorizedAccessException("App user ID not found");
    }

    public static Guid GetPassengerId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("passenger_id");
        return claim is not null ? Guid.Parse(claim) : throw new UnauthorizedAccessException("Passenger ID not found");
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
        => user.HasClaim("permissions", "manage:admin");
}