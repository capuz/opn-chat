namespace opn_chat.Application.DTOs
{
    public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);

    public record AdminStatsDto(
        int TotalUsers,
        int OnlineNow,
        int ActiveRooms,
        int MessagesToday,
        int BannedUsers,
        int PendingReports,
        string ServerUptime,
        int SignalRConnections
    );

    public record OnlineUserLiveDto(
        string Id,
        string Nickname,
        string? CountryCode,
        bool ShowFlag,
        string? AwayMessage,
        string? Badge
    );

    public record ActiveRoomLiveDto(string Id, string Name, int MemberCount);

    public record AdminLiveDataDto(
        IEnumerable<OnlineUserLiveDto> OnlineUsers,
        IEnumerable<ActiveRoomLiveDto> ActiveRooms,
        IEnumerable<AdminMessageDto> RecentMessages
    );

    public record AdminUserDto(
        Guid Id,
        string Nickname,
        string Email,
        string? CountryCode,
        string? GlobalBadge,
        DateTime CreatedAt,
        DateTime LastSeen,
        string? Status,
        bool IsAdmin,
        bool IsDeactivated,
        bool IsBanned,
        DateTime? BanExpiresAt,
        string? BanReason,
        int NicknameChangesToday,
        bool IsOnline
    );

    public record AdminRoomDto(
        Guid Id,
        string Name,
        string? Description,
        bool IsPrivate,
        bool IsLocked,
        string? CreatedByNickname,
        int MemberCount,
        int MessageCount,
        DateTime CreatedAt
    );

    public record AdminMessageDto(
        Guid Id,
        string Content,
        string RoomName,
        Guid RoomId,
        string UserNickname,
        Guid UserId,
        DateTime Timestamp,
        bool IsDeleted,
        int ReportCount
    );

    public record AdminAuditLogDto(
        Guid Id,
        Guid AdminId,
        string AdminNickname,
        string Action,
        string? TargetType,
        string? TargetId,
        string? TargetDisplay,
        string? Details,
        DateTime Timestamp
    );

    public record AdminReportDto(
        Guid Id,
        string ReportedByNickname,
        string? MessageContent,
        Guid? MessageId,
        string? ReportedUserNickname,
        Guid? ReportedUserId,
        string Reason,
        string? Details,
        bool IsResolved,
        DateTime CreatedAt
    );

    public record TopRoomDto(string Name, int MessageCount);

    public record AnalyticsDto(
        int[] DailyMessages,
        int[] DailyActiveUsers,
        string[] DailyLabels,
        TopRoomDto[] TopRooms
    );

    public record SystemSettingDto(string Key, string? Value);

    public record BanUserRequestDto(string Reason, DateTime? ExpiresAt);
    public record AnnounceRequestDto(string Message);
    public record ToggleAdminRequestDto(bool IsAdmin);

    public record CommandPermissionDto(
        string CommandName,
        string Description,
        string Syntax,
        string Category,
        string[] Examples,
        bool MemberAllowed,
        bool OperatorAllowed,
        bool FounderAllowed,
        bool AdminAllowed,
        bool IsDangerous,
        bool IsSystem,
        bool IsDeprecated
    );

    public record UpdateCommandPermissionDto(
        bool MemberAllowed,
        bool OperatorAllowed,
        bool FounderAllowed,
        bool AdminAllowed
    );

    public record AdminMessageSearchParams(
        string? Query,
        string? UserId,
        string? RoomId,
        DateTime? From,
        DateTime? To,
        bool IncludeDeleted,
        int Page,
        int PageSize
    );

    public record AuditLogParams(
        DateTime? From,
        DateTime? To,
        string? Action,
        string? AdminId,
        int Page,
        int PageSize
    );
}
