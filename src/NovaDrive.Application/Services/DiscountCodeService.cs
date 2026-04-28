// Application/Services/DiscountCodeService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public interface IDiscountCodeService
{
    Task<DiscountCodeResponse> CreateAsync(CreateDiscountCodeRequest request);
    Task<DiscountCodeResponse> GetByIdAsync(Guid id);
    Task<DiscountCodeValidationResult> ValidateCodeAsync(string code, decimal currentRideValue);
    Task<PaginatedResponse<DiscountCodeResponse>> GetAllAsync(int page, int pageSize, bool? isActive = null);
    Task<DiscountCodeResponse> UpdateAsync(Guid id, CreateDiscountCodeRequest request);
    Task<DiscountCodeResponse> SetActiveAsync(Guid id, bool isActive);
    Task DeleteAsync(Guid id);
}

public record DiscountCodeValidationResult(bool IsValid, string? Reason, DiscountCodeResponse? DiscountCode);

public class DiscountCodeService : IDiscountCodeService
{
    private readonly IDiscountCodeRepository _discountCodeRepo;

    public DiscountCodeService(IDiscountCodeRepository discountCodeRepo)
    {
        _discountCodeRepo = discountCodeRepo;
    }

    public async Task<DiscountCodeResponse> CreateAsync(CreateDiscountCodeRequest request)
    {
        var existing = await _discountCodeRepo.GetByCodeAsync(request.Code);
        if (existing is not null)
            throw new InvalidOperationException($"Discount code '{request.Code}' already exists");

        var code = new DiscountCode
        {
            Code = request.Code.ToUpper(),
            Type = Enum.Parse<DiscountType>(request.Type),
            Value = request.Value,
            MinimumRideValue = request.MinimumRideValue,
            ExpiresAt = request.ExpiresAt,
            MaxUses = request.MaxUses
        };

        await _discountCodeRepo.CreateAsync(code);
        return code.ToResponse();
    }

    public async Task<DiscountCodeResponse> GetByIdAsync(Guid id)
    {
        var code = await _discountCodeRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Discount code {id} not found");
        return code.ToResponse();
    }

    public async Task<DiscountCodeValidationResult> ValidateCodeAsync(string code, decimal currentRideValue)
    {
        var discountCode = await _discountCodeRepo.GetByCodeAsync(code);

        if (discountCode is null)
            return new(false, "Discount code not found", null);

        if (!discountCode.IsActive)
            return new(false, "Discount code is inactive", null);

        if (DateTime.UtcNow >= discountCode.ExpiresAt)
            return new(false, "Discount code has expired", null);

        if (discountCode.MaxUses.HasValue && discountCode.TimesUsed >= discountCode.MaxUses.Value)
            return new(false, "Discount code has reached its usage limit", null);

        if (currentRideValue < discountCode.MinimumRideValue)
            return new(false, $"Ride value must be at least €{discountCode.MinimumRideValue:F2}", null);

        return new(true, null, discountCode.ToResponse());
    }

    public async Task<PaginatedResponse<DiscountCodeResponse>> GetAllAsync(int page, int pageSize, bool? isActive = null)
    {
        var codes = await _discountCodeRepo.GetAllAsync(page, pageSize, isActive);
        var total = await _discountCodeRepo.GetTotalCountAsync(isActive);

        return new PaginatedResponse<DiscountCodeResponse>
        {
            Items = codes.Select(c => c.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<DiscountCodeResponse> UpdateAsync(Guid id, CreateDiscountCodeRequest request)
    {
        var code = await _discountCodeRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Discount code {id} not found");

        code.Code = request.Code.ToUpper();
        code.Type = Enum.Parse<DiscountType>(request.Type);
        code.Value = request.Value;
        code.MinimumRideValue = request.MinimumRideValue;
        code.ExpiresAt = request.ExpiresAt;
        code.MaxUses = request.MaxUses;

        await _discountCodeRepo.UpdateAsync(code);
        return code.ToResponse();
    }

    public async Task<DiscountCodeResponse> SetActiveAsync(Guid id, bool isActive)
    {
        var code = await _discountCodeRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Discount code {id} not found");
        code.IsActive = isActive;
        await _discountCodeRepo.UpdateAsync(code);
        return code.ToResponse();
    }

    public async Task DeleteAsync(Guid id)
    {
        _ = await _discountCodeRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Discount code {id} not found");
        await _discountCodeRepo.DeleteAsync(id);
    }
}