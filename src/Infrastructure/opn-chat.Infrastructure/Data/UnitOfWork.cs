using opn_chat.Domain.Interfaces;

namespace opn_chat.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public Task<int> CommitAsync() => _context.SaveChangesAsync();
    }
}
