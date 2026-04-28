// Infrastructure/Repositories/UserRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByAuth0IdAsync(string auth0Id);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
}

public class UserRepository : IUserRepository
{
    private readonly NovaDriveDbContext _context;

    public UserRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByIdAsync(Guid id)
        => await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByAuth0IdAsync(string auth0Id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }
    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize)
        => await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalCountAsync()
        => await _context.Users.CountAsync();
}