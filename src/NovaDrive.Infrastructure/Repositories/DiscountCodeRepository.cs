// Infrastructure/Repositories/DiscountCodeRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IDiscountCodeRepository
{
    Task<DiscountCode?> GetByIdAsync(Guid id);
    Task<DiscountCode?> GetByCodeAsync(string code);
    Task<DiscountCode> CreateAsync(DiscountCode discountCode);
    Task<DiscountCode> UpdateAsync(DiscountCode discountCode);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<DiscountCode>> GetAllAsync(int page, int pageSize, bool? isActive = null);
    Task<int> GetTotalCountAsync(bool? isActive = null);
    Task IncrementUsageAsync(Guid id);
}

public class DiscountCodeRepository : IDiscountCodeRepository
{
    private readonly NovaDriveDbContext _context;

    public DiscountCodeRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<DiscountCode?> GetByIdAsync(Guid id)
        => await _context.DiscountCodes.FindAsync(id);

    public async Task<DiscountCode?> GetByCodeAsync(string code)
        => await _context.DiscountCodes
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == code.ToUpper());

    public async Task<DiscountCode> CreateAsync(DiscountCode discountCode)
    {
        _context.DiscountCodes.Add(discountCode);
        await _context.SaveChangesAsync();
        return discountCode;
    }

    public async Task<DiscountCode> UpdateAsync(DiscountCode discountCode)
    {
        _context.DiscountCodes.Update(discountCode);
        await _context.SaveChangesAsync();
        return discountCode;
    }

    public async Task DeleteAsync(Guid id)
    {
        var code = await _context.DiscountCodes.FindAsync(id);
        if (code is not null)
        {
            _context.DiscountCodes.Remove(code);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<DiscountCode>> GetAllAsync(int page, int pageSize, bool? isActive = null)
    {
        var query = _context.DiscountCodes.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(bool? isActive = null)
    {
        var query = _context.DiscountCodes.AsQueryable();
        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive.Value);
        return await query.CountAsync();
    }

    public async Task IncrementUsageAsync(Guid id)
    {
        await _context.DiscountCodes
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(d => d.SetProperty(
                x => x.TimesUsed,
                x => x.TimesUsed + 1));
    }
}