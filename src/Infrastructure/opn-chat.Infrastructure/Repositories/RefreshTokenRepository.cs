using Microsoft.EntityFrameworkCore;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByIdAsync(Guid id)
        {
            return await _context.RefreshTokens.FindAsync(id);
        }

        public async Task<IEnumerable<RefreshToken>> GetAllAsync()
        {
            return await _context.RefreshTokens.ToListAsync();
        }

        public async Task AddAsync(RefreshToken entity)
        {
            await _context.RefreshTokens.AddAsync(entity);
        }

        public Task UpdateAsync(RefreshToken entity)
        {
            _context.RefreshTokens.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(RefreshToken entity)
        {
            _context.RefreshTokens.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        }
    }
}
