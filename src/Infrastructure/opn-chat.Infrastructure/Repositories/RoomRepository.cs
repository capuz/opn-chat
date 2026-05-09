using Microsoft.EntityFrameworkCore;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Room?> GetByIdAsync(Guid id)
        {
            return await _context.Rooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Room>> GetAllAsync()
        {
            return await _context.Rooms
                .Where(r => !r.IsPrivate)
                .Include(r => r.Members)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetPublicRoomsAsync()
        {
            return await _context.Rooms
                .Where(r => !r.IsPrivate)
                .Include(r => r.Members)
                .ToListAsync();
        }

        public async Task AddAsync(Room entity)
        {
            await _context.Rooms.AddAsync(entity);
        }

        public Task UpdateAsync(Room entity)
        {
            _context.Rooms.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Room entity)
        {
            _context.Rooms.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> IsUserMemberAsync(Guid userId, Guid roomId)
        {
            return await _context.RoomMembers
                .AnyAsync(rm => rm.UserId == userId && rm.RoomId == roomId);
        }
    }
}
