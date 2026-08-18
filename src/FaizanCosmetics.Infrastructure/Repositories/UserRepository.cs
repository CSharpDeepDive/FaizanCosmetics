using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    public UserRepository(ApplicationDbContext context) => _context = context;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync(cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(u => u.Username == username && (excludeUserId == null || u.Id != excludeUserId), cancellationToken);

    public void Add(User user) => _context.Users.Add(user);
    public void Update(User user) => _context.Users.Update(user);
}
