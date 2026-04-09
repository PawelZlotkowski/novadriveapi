// Api/Endpoints/Admin/AdminUserEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Domain.Enums;
using NovaDrive.Infrastructure.Repositories;

public static class AdminUserEndpoints
{
    public static RouteGroupBuilder MapAdminUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IUserRepository repo, int page = 1, int pageSize = 20) =>
        {
            var users = await repo.GetAllAsync(page, pageSize);
            var total = await repo.GetTotalCountAsync();
            return Results.Ok(new { items = users, page, pageSize, totalCount = total });
        });

        group.MapGet("/{id:guid}", async (Guid id, IUserRepository repo) =>
        {
            var user = await repo.GetByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        });

        group.MapPut("/{id:guid}/role", async (Guid id, ChangeRoleRequest request, IUserRepository repo) =>
        {
            var user = await repo.GetByIdAsync(id);
            if (user is null) return Results.NotFound();
            user.Role = Enum.Parse<UserRole>(request.Role);
            await repo.UpdateAsync(user);
            return Results.Ok(new { user.Id, Role = user.Role.ToString() });
        });

        group.MapPut("/{id:guid}/status", async (Guid id, ChangeStatusRequest request, IUserRepository repo) =>
        {
            var user = await repo.GetByIdAsync(id);
            if (user is null) return Results.NotFound();
            user.IsActive = request.IsActive;
            await repo.UpdateAsync(user);
            return Results.Ok(new { user.Id, user.IsActive });
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Users");
    }
}

public record ChangeRoleRequest(string Role);
public record ChangeStatusRequest(bool IsActive);