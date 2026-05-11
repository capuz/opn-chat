using Microsoft.EntityFrameworkCore;
using opn_chat.Application.DTOs;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;

namespace opn_chat.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _db;
        private readonly IPresenceTracker _presence;
        private readonly ICommandPermissionRepository _commandPermissionRepo;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public AdminService(AppDbContext db, IPresenceTracker presence, ICommandPermissionRepository commandPermissionRepo)
        {
            _db = db;
            _presence = presence;
            _commandPermissionRepo = commandPermissionRepo;
        }

        public async Task<AdminStatsDto> GetStatsAsync()
        {
            var totalUsers = await _db.Users.CountAsync();
            var onlineNow = _presence.GetOnlineUsers().Count;
            var activeRooms = await _db.Rooms.CountAsync();
            var today = DateTime.UtcNow.Date;
            var messagesToday = await _db.Messages.IgnoreQueryFilters().CountAsync(m => m.Timestamp >= today);
            var bannedUsers = await _db.Bans.CountAsync(b => b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow));
            var pendingReports = await _db.Reports.CountAsync(r => !r.IsResolved);
            var uptime = DateTime.UtcNow - _startTime;
            var uptimeStr = uptime.Days > 0
                ? $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m"
                : $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            return new AdminStatsDto(totalUsers, onlineNow, activeRooms, messagesToday, bannedUsers, pendingReports, uptimeStr, onlineNow);
        }

        public async Task<AdminLiveDataDto> GetLiveDataAsync()
        {
            var onlineUsers = _presence.GetOnlineUsers()
                .Select(u => new OnlineUserLiveDto(u.Id, u.Nickname, u.CountryCode, u.ShowFlag, u.AwayMessage, u.Badge));

            var activeRooms = await _db.Rooms
                .Select(r => new ActiveRoomLiveDto(r.Id.ToString(), r.Name, r.Members.Count))
                .ToListAsync();

            var recentMessages = await _db.Messages.IgnoreQueryFilters()
                .Include(m => m.User)
                .Include(m => m.Room)
                .OrderByDescending(m => m.Timestamp)
                .Take(20)
                .Select(m => new AdminMessageDto(
                    m.Id, m.Content, m.Room.Name, m.RoomId,
                    m.User.Nickname, m.UserId, m.Timestamp, m.IsDeleted, m.Reports.Count))
                .ToListAsync();

            return new AdminLiveDataDto(onlineUsers, activeRooms, recentMessages);
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? search)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Nickname.Contains(search) || u.Email.Contains(search));

            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id, u.Nickname, u.Email, u.CountryCode, u.GlobalBadge,
                    u.CreatedAt, u.LastSeen, u.Status, u.IsAdmin, u.IsDeactivated, u.NicknameChangesToday,
                    ActiveBan = u.BansReceived
                        .Where(b => b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow))
                        .OrderByDescending(b => b.BannedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var onlineIds = _presence.GetOnlineUsers().Select(u => u.Id).ToHashSet();

            var items = rows.Select(u => new AdminUserDto(
                u.Id, u.Nickname, u.Email, u.CountryCode, u.GlobalBadge,
                u.CreatedAt, u.LastSeen, u.Status, u.IsAdmin, u.IsDeactivated,
                u.ActiveBan != null, u.ActiveBan?.ExpiresAt, u.ActiveBan?.Reason,
                u.NicknameChangesToday, onlineIds.Contains(u.Id.ToString())));

            return new PagedResult<AdminUserDto>(items, total, page, pageSize);
        }

        public async Task BanUserAsync(Guid adminId, string adminNick, Guid userId, BanUserRequestDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;
            _db.Bans.Add(new Ban { UserId = userId, BannedById = adminId, Reason = dto.Reason, ExpiresAt = dto.ExpiresAt, IsActive = true });
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "BanUser", "user", userId.ToString(), user.Nickname,
                $"Reason: {dto.Reason}. Expires: {dto.ExpiresAt?.ToString("o") ?? "never"}");
        }

        public async Task UnbanUserAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var bans = await _db.Bans.Where(b => b.UserId == userId && b.IsActive).ToListAsync();
            foreach (var b in bans) b.IsActive = false;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "UnbanUser", "user", userId.ToString(), user?.Nickname, null);
        }

        public async Task LogKickAsync(Guid adminId, string adminNick, Guid userId, string targetNick)
        {
            await LogActionAsync(adminId, adminNick, "KickUser", "user", userId.ToString(), targetNick, null);
        }

        public async Task MuteUserAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var members = await _db.RoomMembers.Where(rm => rm.UserId == userId).ToListAsync();
            foreach (var m in members) m.IsMuted = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "MuteUser", "user", userId.ToString(), user?.Nickname, null);
        }

        public async Task UnmuteUserAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var members = await _db.RoomMembers.Where(rm => rm.UserId == userId).ToListAsync();
            foreach (var m in members) m.IsMuted = false;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "UnmuteUser", "user", userId.ToString(), user?.Nickname, null);
        }

        public async Task ForceLogoutAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
            foreach (var t in tokens) t.IsRevoked = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "ForceLogout", "user", userId.ToString(), user?.Nickname, $"Revoked {tokens.Count} tokens");
        }

        public async Task ToggleAdminAsync(Guid adminId, string adminNick, Guid userId, bool isAdmin)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;
            user.IsAdmin = isAdmin;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, isAdmin ? "GrantAdmin" : "RevokeAdmin", "user", userId.ToString(), user.Nickname, null);
        }

        public async Task ResetNicknameChangesAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;
            user.NicknameChangesToday = 0;
            user.NicknameChangesDate = null;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "ResetNicknameChanges", "user", userId.ToString(), user.Nickname, null);
        }

        public async Task DeactivateUserAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;
            user.IsDeactivated = !user.IsDeactivated;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, user.IsDeactivated ? "DeactivateUser" : "ReactivateUser", "user", userId.ToString(), user.Nickname, null);
        }

        public async Task<IEnumerable<AdminRoomDto>> GetRoomsAsync()
        {
            return await _db.Rooms
                .Include(r => r.CreatedBy)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AdminRoomDto(
                    r.Id, r.Name, r.Description, r.IsPrivate, r.IsLocked,
                    r.CreatedBy != null ? r.CreatedBy.Nickname : null,
                    r.Members.Count, r.Messages.Count, r.CreatedAt))
                .ToListAsync();
        }

        public async Task LockRoomAsync(Guid adminId, string adminNick, Guid roomId)
        {
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null) return;
            room.IsLocked = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "LockRoom", "room", roomId.ToString(), room.Name, null);
        }

        public async Task UnlockRoomAsync(Guid adminId, string adminNick, Guid roomId)
        {
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null) return;
            room.IsLocked = false;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "UnlockRoom", "room", roomId.ToString(), room.Name, null);
        }

        public async Task DeleteRoomAsync(Guid adminId, string adminNick, Guid roomId)
        {
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null) return;
            var name = room.Name;
            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "DeleteRoom", "room", roomId.ToString(), name, null);
        }

        public async Task ClearRoomMessagesAsync(Guid adminId, string adminNick, Guid roomId)
        {
            var room = await _db.Rooms.FindAsync(roomId);
            var msgs = await _db.Messages.IgnoreQueryFilters().Where(m => m.RoomId == roomId).ToListAsync();
            foreach (var m in msgs) m.IsDeleted = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "ClearRoomMessages", "room", roomId.ToString(), room?.Name, $"Cleared {msgs.Count} messages");
        }

        public async Task<PagedResult<AdminMessageDto>> SearchMessagesAsync(AdminMessageSearchParams p)
        {
            var query = _db.Messages.IgnoreQueryFilters()
                .Include(m => m.User).Include(m => m.Room).AsQueryable();

            if (!string.IsNullOrWhiteSpace(p.Query)) query = query.Where(m => m.Content.Contains(p.Query));
            if (!string.IsNullOrWhiteSpace(p.UserId) && Guid.TryParse(p.UserId, out var uid)) query = query.Where(m => m.UserId == uid);
            if (!string.IsNullOrWhiteSpace(p.RoomId) && Guid.TryParse(p.RoomId, out var rid)) query = query.Where(m => m.RoomId == rid);
            if (p.From.HasValue) query = query.Where(m => m.Timestamp >= p.From.Value);
            if (p.To.HasValue) query = query.Where(m => m.Timestamp <= p.To.Value);
            if (!p.IncludeDeleted) query = query.Where(m => !m.IsDeleted);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.Timestamp)
                .Skip((p.Page - 1) * p.PageSize).Take(p.PageSize)
                .Select(m => new AdminMessageDto(m.Id, m.Content, m.Room.Name, m.RoomId, m.User.Nickname, m.UserId, m.Timestamp, m.IsDeleted, m.Reports.Count))
                .ToListAsync();

            return new PagedResult<AdminMessageDto>(items, total, p.Page, p.PageSize);
        }

        public async Task DeleteMessageAsync(Guid adminId, string adminNick, Guid messageId)
        {
            var msg = await _db.Messages.IgnoreQueryFilters().Include(m => m.User).FirstOrDefaultAsync(m => m.Id == messageId);
            if (msg == null) return;
            msg.IsDeleted = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "DeleteMessage", "message", messageId.ToString(), msg.User.Nickname, msg.Content[..Math.Min(50, msg.Content.Length)]);
        }

        public async Task BulkDeleteUserMessagesAsync(Guid adminId, string adminNick, Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var msgs = await _db.Messages.IgnoreQueryFilters().Where(m => m.UserId == userId && !m.IsDeleted).ToListAsync();
            foreach (var m in msgs) m.IsDeleted = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "BulkDeleteMessages", "user", userId.ToString(), user?.Nickname, $"Deleted {msgs.Count} messages");
        }

        public async Task<PagedResult<AdminReportDto>> GetReportsAsync(bool unresolvedOnly, int page, int pageSize)
        {
            var query = _db.Reports
                .Include(r => r.ReportedBy)
                .Include(r => r.Message).ThenInclude(m => m!.User)
                .AsQueryable();
            if (unresolvedOnly) query = query.Where(r => !r.IsResolved);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new AdminReportDto(
                    r.Id, r.ReportedBy.Nickname,
                    r.Message != null ? r.Message.Content : null,
                    r.MessageId,
                    r.Message != null ? r.Message.User.Nickname : null,
                    r.Message != null ? (Guid?)r.Message.UserId : null,
                    r.Reason, r.Details, r.IsResolved, r.CreatedAt))
                .ToListAsync();

            return new PagedResult<AdminReportDto>(items, total, page, pageSize);
        }

        public async Task ResolveReportAsync(Guid adminId, string adminNick, Guid reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report == null) return;
            report.IsResolved = true;
            await _db.SaveChangesAsync();
            await LogActionAsync(adminId, adminNick, "ResolveReport", "report", reportId.ToString(), null, null);
        }

        public async Task<PagedResult<AdminAuditLogDto>> GetAuditLogsAsync(AuditLogParams p)
        {
            var query = _db.AdminAuditLogs.AsQueryable();
            if (p.From.HasValue) query = query.Where(l => l.Timestamp >= p.From.Value);
            if (p.To.HasValue) query = query.Where(l => l.Timestamp <= p.To.Value);
            if (!string.IsNullOrWhiteSpace(p.Action)) query = query.Where(l => l.Action == p.Action);
            if (!string.IsNullOrWhiteSpace(p.AdminId) && Guid.TryParse(p.AdminId, out var aid)) query = query.Where(l => l.AdminId == aid);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((p.Page - 1) * p.PageSize).Take(p.PageSize)
                .Select(l => new AdminAuditLogDto(l.Id, l.AdminId, l.AdminNickname, l.Action, l.TargetType, l.TargetId, l.TargetDisplay, l.Details, l.Timestamp))
                .ToListAsync();

            return new PagedResult<AdminAuditLogDto>(items, total, p.Page, p.PageSize);
        }

        public async Task<AnalyticsDto> GetAnalyticsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6);

            var msgByDay = await _db.Messages.IgnoreQueryFilters()
                .Where(m => m.Timestamp >= sevenDaysAgo)
                .GroupBy(m => m.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var usersByDay = await _db.Messages.IgnoreQueryFilters()
                .Where(m => m.Timestamp >= sevenDaysAgo)
                .GroupBy(m => m.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Select(m => m.UserId).Distinct().Count() })
                .ToListAsync();

            var labels = new string[7];
            var dailyMessages = new int[7];
            var dailyUsers = new int[7];
            for (var i = 0; i < 7; i++)
            {
                var d = sevenDaysAgo.AddDays(i);
                labels[i] = d.ToString("MMM d");
                dailyMessages[i] = msgByDay.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
                dailyUsers[i] = usersByDay.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
            }

            var topRooms = await _db.Rooms
                .OrderByDescending(r => r.Messages.Count)
                .Take(5)
                .Select(r => new TopRoomDto(r.Name, r.Messages.Count))
                .ToListAsync();

            return new AnalyticsDto(dailyMessages, dailyUsers, labels, topRooms.ToArray());
        }

        public async Task<IEnumerable<SystemSettingDto>> GetSettingsAsync()
        {
            return await _db.SystemSettings
                .Select(s => new SystemSettingDto(s.Key, s.Value))
                .ToListAsync();
        }

        public async Task UpdateSettingsAsync(IEnumerable<SystemSettingDto> settings)
        {
            foreach (var dto in settings)
            {
                var setting = await _db.SystemSettings.FindAsync(dto.Key);
                if (setting != null) setting.Value = dto.Value;
                else _db.SystemSettings.Add(new SystemSetting { Key = dto.Key, Value = dto.Value });
            }
            await _db.SaveChangesAsync();
        }

        public async Task LogAnnouncementAsync(Guid adminId, string adminNick, string message)
        {
            await LogActionAsync(adminId, adminNick, "GlobalAnnouncement", "system", null, null, message);
        }

        public async Task<string?> GetUserNicknameAsync(Guid userId)
        {
            return await _db.Users.Where(u => u.Id == userId).Select(u => u.Nickname).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CommandPermissionDto>> GetCommandPermissionsAsync()
        {
            var all = await _commandPermissionRepo.GetAllAsync();
            return all.Select(p => new CommandPermissionDto(
                p.CommandName, p.Description, p.Syntax, p.Category,
                p.Examples.Split(';', StringSplitOptions.RemoveEmptyEntries),
                p.MemberAllowed, p.OperatorAllowed, p.FounderAllowed, p.AdminAllowed,
                p.IsDangerous, p.IsSystem, p.IsDeprecated
            ));
        }

        public async Task UpdateCommandPermissionAsync(string commandName, UpdateCommandPermissionDto dto, Guid adminId, string adminNick)
        {
            var existing = await _commandPermissionRepo.GetByNameAsync(commandName);
            if (existing == null) return;
            existing.MemberAllowed   = dto.MemberAllowed;
            existing.OperatorAllowed = dto.OperatorAllowed;
            existing.FounderAllowed  = dto.FounderAllowed;
            existing.AdminAllowed    = dto.AdminAllowed;
            await _commandPermissionRepo.UpsertAsync(existing);
            await LogActionAsync(adminId, adminNick, "UpdateCommandPermission", "command", commandName, commandName,
                $"Member={dto.MemberAllowed} Operator={dto.OperatorAllowed} Founder={dto.FounderAllowed} Admin={dto.AdminAllowed}");
        }

        public async Task ResetCommandPermissionsAsync(Guid adminId, string adminNick)
        {
            await _commandPermissionRepo.UpsertManyAsync(DefaultPermissions());
            await LogActionAsync(adminId, adminNick, "ResetCommandPermissions", "system", null, null, "Permissions reset to defaults");
        }

        public async Task<bool> CanExecuteAsync(string commandName, Guid roleId, bool isAdmin)
        {
            var perm = await _commandPermissionRepo.GetByNameAsync(commandName);
            if (perm == null) return false;
            if (isAdmin) return perm.AdminAllowed;
            if (roleId == RoleIds.Owner)     return perm.FounderAllowed;
            if (roleId == RoleIds.Moderator) return perm.OperatorAllowed;
            return perm.MemberAllowed;
        }

        public static IEnumerable<CommandPermission> DefaultPermissions() => new[]
        {
            Perm("join",        "Switch to a room",                     "/join #roomname",           "Messaging",        "join #general;join #random",                       m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("msg",         "Open a direct message",                "/msg <nick> [message]",     "Messaging",        "msg john123;msg alice hello there",                 m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("query",       "Open a query window with a user",      "/query <nick>",             "Messaging",        "query john123",                                    m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("nick",        "Change your display name",             "/nick <nickname>",          "Identity",         "nick cooluser;nick my_new_name",                    m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("away",        "Set yourself as away",                 "/away [message]",           "Identity",         "away;away be back in 10 min",                      m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("ignore",      "Ignore messages from a user",          "/ignore <nick>",            "Messaging",        "ignore spammer42",                                 m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("kick",        "Kick a user from this room",           "/kick <nick>",              "Moderation",       "kick john123;kick spambot",                        m:false, op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("ban",         "Ban a user from this room",            "/ban <nick>",               "Moderation",       "ban spammer42;ban troll99",                        m:false, op:true,  fo:true,  ad:true,  danger:true,  sys:false),
            Perm("topic",       "Set the room topic / description",     "/topic <text>",             "Room Management",  "topic Welcome to #general!;topic Off-topic chat",  m:false, op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("mute",        "Mute a user in this room",             "/mute <nick>",              "Moderation",       "mute louduser;mute spammer",                       m:false, op:true,  fo:true,  ad:true,  danger:false, sys:false),
            Perm("delete-room", "Permanently delete a room",            "/delete-room",              "Room Management",  "",                                                 m:false, op:false, fo:true,  ad:true,  danger:true,  sys:false),
            Perm("create-room", "Create a new IRC-style room",          "/create-room #roomname",    "Room Management",  "create-room #myroom;create-room #project-alpha",   m:true,  op:true,  fo:true,  ad:true,  danger:false, sys:false),
        };

        private static CommandPermission Perm(
            string name, string desc, string syntax, string category, string examples,
            bool m, bool op, bool fo, bool ad, bool danger, bool sys) =>
            new CommandPermission
            {
                CommandName = name, Description = desc, Syntax = syntax, Category = category,
                Examples = examples, MemberAllowed = m, OperatorAllowed = op,
                FounderAllowed = fo, AdminAllowed = ad, IsDangerous = danger, IsSystem = sys
            };

        private async Task LogActionAsync(Guid adminId, string adminNick, string action,
            string? targetType, string? targetId, string? targetDisplay, string? details)
        {
            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminId = adminId,
                AdminNickname = adminNick,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TargetDisplay = targetDisplay,
                Details = details
            });
            await _db.SaveChangesAsync();
        }
    }
}
