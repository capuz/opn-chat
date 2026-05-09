using Microsoft.EntityFrameworkCore;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Repositories
{
    public class RoomMemberRepository : IRoomMemberRepository
    {
        private readonly AppDbContext _context;

        public RoomMemberRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RoomMember?> GetByIdAsync(Guid id)
        {
            return await _context.RoomMembers.FindAsync(id);
        }

        public async Task<IEnumerable<RoomMember>> GetAllAsync()
        {
            return await _context.RoomMembers.ToListAsync();
        }

        public async Task AddAsync(RoomMember entity)
        {
            await _context.RoomMembers.AddAsync(entity);
        }

        public Task UpdateAsync(RoomMember entity)
        {
            _context.RoomMembers.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(RoomMember entity)
        {
            _context.RoomMembers.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<RoomMember>> GetRoomMembersAsync(Guid roomId)
        {
            return await _context.RoomMembers
                .Where(rm => rm.RoomId == roomId)
                .Include(rm => rm.User)
                .Include(rm => rm.Role)
                .ToListAsync();
        }

        public async Task<RoomMember?> GetUserRoomMemberAsync(Guid userId, Guid roomId)
        {
            return await _context.RoomMembers
                .Include(rm => rm.Role)
                .FirstOrDefaultAsync(rm => rm.UserId == userId && rm.RoomId == roomId);
        }

        public async Task<bool> IsUserMemberAsync(Guid userId, Guid roomId)
        {
            return await _context.RoomMembers
                .AnyAsync(rm => rm.UserId == userId && rm.RoomId == roomId);
        }
    }
}
