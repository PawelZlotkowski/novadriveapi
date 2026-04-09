// Api/Endpoints/Admin/AdminDiscountCodeEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;

public static class AdminDiscountCodeEndpoints
{
    public static RouteGroupBuilder MapAdminDiscountCodeEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IDiscountCodeService service, int page = 1, int pageSize = 20, bool? isActive = null) =>
            Results.Ok(await service.GetAllAsync(page, pageSize, isActive)));

        group.MapGet("/{id:guid}", async (Guid id, IDiscountCodeService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapPost("/", async (
            CreateDiscountCodeRequest request,
            IDiscountCodeService service,
            IValidator<CreateDiscountCodeRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.UnprocessableEntity(validation.Errors);
            var code = await service.CreateAsync(request);
            return Results.Created($"/api/admin/discount-codes/{code.Id}", code);
        });

        group.MapPut("/{id:guid}", async (Guid id, CreateDiscountCodeRequest request, IDiscountCodeService service) =>
            Results.Ok(await service.UpdateAsync(id, request)));

        group.MapDelete("/{id:guid}", async (Guid id, IDiscountCodeService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Discount Codes");
    }
}