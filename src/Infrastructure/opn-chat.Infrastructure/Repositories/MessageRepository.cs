using Microsoft.EntityFrameworkCore;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> GetByIdAsync(Guid id)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.ReplyTo)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Message>> GetAllAsync()
        {
            return await _context.Messages
                .Include(m => m.User)
                .ToListAsync();
        }

        public async Task AddAsync(Message entity)
        {
            await _context.Messages.AddAsync(entity);
        }

        public Task UpdateAsync(Message entity)
        {
            _context.Messages.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Message entity)
        {
            _context.Messages.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Message>> GetRoomMessagesAsync(Guid roomId, int skip, int take)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.ReplyTo)
                .Where(m => m.RoomId == roomId && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .Skip(skip)
                .Take(take)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
}
