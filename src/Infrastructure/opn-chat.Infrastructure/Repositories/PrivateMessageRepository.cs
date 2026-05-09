using Microsoft.EntityFrameworkCore;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Repositories
{
    public class PrivateMessageRepository : IPrivateMessageRepository
    {
        private readonly AppDbContext _context;

        public PrivateMessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PrivateMessage?> GetByIdAsync(Guid id)
        {
            return await _context.PrivateMessages
                .Include(pm => pm.Sender)
                .Include(pm => pm.Receiver)
                .FirstOrDefaultAsync(pm => pm.Id == id);
        }

        public async Task<IEnumerable<PrivateMessage>> GetAllAsync()
        {
            return await _context.PrivateMessages
                .Include(pm => pm.Sender)
                .Include(pm => pm.Receiver)
                .ToListAsync();
        }

        public async Task AddAsync(PrivateMessage entity)
        {
            await _context.PrivateMessages.AddAsync(entity);
        }

        public Task UpdateAsync(PrivateMessage entity)
        {
            _context.PrivateMessages.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PrivateMessage entity)
        {
            _context.PrivateMessages.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<PrivateMessage>> GetConversationAsync(Guid user1Id, Guid user2Id, Guid requesterId, int skip, int take)
        {
            return await _context.PrivateMessages
                .Where(pm =>
                    ((pm.SenderId == user1Id && pm.ReceiverId == user2Id) ||
                     (pm.SenderId == user2Id && pm.ReceiverId == user1Id)) &&
                    !(pm.IsDeletedBySender && pm.SenderId == requesterId && !pm.IsDeletedForEveryone) &&
                    !(pm.IsDeletedByReceiver && pm.ReceiverId == requesterId && !pm.IsDeletedForEveryone))
                .Include(pm => pm.Sender)
                .Include(pm => pm.Receiver)
                .OrderByDescending(pm => pm.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.PrivateMessages
                .CountAsync(pm => pm.ReceiverId == userId && !pm.IsRead);
        }

        public async Task MarkConversationAsReadAsync(Guid senderId, Guid receiverId)
        {
            var unread = await _context.PrivateMessages
                .Where(pm => pm.SenderId == senderId && pm.ReceiverId == receiverId && !pm.IsRead)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var msg in unread)
            {
                msg.IsRead = true;
                msg.ReadAt = now;
            }
        }
    }
}
