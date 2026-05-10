using Microsoft.EntityFrameworkCore;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Repositories
{
    public class CommandPermissionRepository : ICommandPermissionRepository
    {
        private readonly AppDbContext _db;

        public CommandPermissionRepository(AppDbContext db) => _db = db;

        public Task<IEnumerable<CommandPermission>> GetAllAsync() =>
            Task.FromResult<IEnumerable<CommandPermission>>(
                _db.CommandPermissions.AsNoTracking().OrderBy(c => c.Category).ThenBy(c => c.CommandName).ToList()
            );

        public Task<CommandPermission?> GetByNameAsync(string commandName) =>
            _db.CommandPermissions.AsNoTracking()
               .FirstOrDefaultAsync(c => c.CommandName == commandName)!;

        public async Task UpsertAsync(CommandPermission permission)
        {
            var existing = await _db.CommandPermissions.FindAsync(permission.CommandName);
            if (existing == null)
                _db.CommandPermissions.Add(permission);
            else
            {
                existing.MemberAllowed   = permission.MemberAllowed;
                existing.OperatorAllowed = permission.OperatorAllowed;
                existing.FounderAllowed  = permission.FounderAllowed;
                existing.AdminAllowed    = permission.AdminAllowed;
                existing.Description     = permission.Description;
                existing.Syntax          = permission.Syntax;
                existing.Category        = permission.Category;
                existing.Examples        = permission.Examples;
                existing.IsDangerous     = permission.IsDangerous;
                existing.IsSystem        = permission.IsSystem;
                existing.IsDeprecated    = permission.IsDeprecated;
            }
            await _db.SaveChangesAsync();
        }

        public async Task UpsertManyAsync(IEnumerable<CommandPermission> permissions)
        {
            foreach (var permission in permissions)
            {
                var existing = await _db.CommandPermissions.FindAsync(permission.CommandName);
                if (existing == null)
                    _db.CommandPermissions.Add(permission);
                else
                {
                    existing.MemberAllowed   = permission.MemberAllowed;
                    existing.OperatorAllowed = permission.OperatorAllowed;
                    existing.FounderAllowed  = permission.FounderAllowed;
                    existing.AdminAllowed    = permission.AdminAllowed;
                }
            }
            await _db.SaveChangesAsync();
        }
    }
}
