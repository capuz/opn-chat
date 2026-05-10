using opn_chat.Application.DTOs;

namespace opn_chat.Application.Interfaces
{
    public interface IAdminService
    {
        Task<AdminStatsDto> GetStatsAsync();
        Task<AdminLiveDataDto> GetLiveDataAsync();
        Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? search);
        Task BanUserAsync(Guid adminId, string adminNick, Guid userId, BanUserRequestDto dto);
        Task UnbanUserAsync(Guid adminId, string adminNick, Guid userId);
        Task LogKickAsync(Guid adminId, string adminNick, Guid userId, string targetNick);
        Task MuteUserAsync(Guid adminId, string adminNick, Guid userId);
        Task UnmuteUserAsync(Guid adminId, string adminNick, Guid userId);
        Task ForceLogoutAsync(Guid adminId, string adminNick, Guid userId);
        Task ToggleAdminAsync(Guid adminId, string adminNick, Guid userId, bool isAdmin);
        Task ResetNicknameChangesAsync(Guid adminId, string adminNick, Guid userId);
        Task DeactivateUserAsync(Guid adminId, string adminNick, Guid userId);
        Task<IEnumerable<AdminRoomDto>> GetRoomsAsync();
        Task LockRoomAsync(Guid adminId, string adminNick, Guid roomId);
        Task UnlockRoomAsync(Guid adminId, string adminNick, Guid roomId);
        Task DeleteRoomAsync(Guid adminId, string adminNick, Guid roomId);
        Task ClearRoomMessagesAsync(Guid adminId, string adminNick, Guid roomId);
        Task<PagedResult<AdminMessageDto>> SearchMessagesAsync(AdminMessageSearchParams p);
        Task DeleteMessageAsync(Guid adminId, string adminNick, Guid messageId);
        Task BulkDeleteUserMessagesAsync(Guid adminId, string adminNick, Guid userId);
        Task<PagedResult<AdminReportDto>> GetReportsAsync(bool unresolvedOnly, int page, int pageSize);
        Task ResolveReportAsync(Guid adminId, string adminNick, Guid reportId);
        Task<PagedResult<AdminAuditLogDto>> GetAuditLogsAsync(AuditLogParams p);
        Task<AnalyticsDto> GetAnalyticsAsync();
        Task<IEnumerable<SystemSettingDto>> GetSettingsAsync();
        Task UpdateSettingsAsync(IEnumerable<SystemSettingDto> settings);
        Task LogAnnouncementAsync(Guid adminId, string adminNick, string message);
        Task<string?> GetUserNicknameAsync(Guid userId);
        Task<IEnumerable<CommandPermissionDto>> GetCommandPermissionsAsync();
        Task UpdateCommandPermissionAsync(string commandName, UpdateCommandPermissionDto dto, Guid adminId, string adminNick);
        Task ResetCommandPermissionsAsync(Guid adminId, string adminNick);
        Task<bool> CanExecuteAsync(string commandName, Guid roleId, bool isAdmin);
    }
}
