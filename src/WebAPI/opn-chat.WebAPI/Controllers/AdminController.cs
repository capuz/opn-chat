using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.DTOs;
using opn_chat.Application.Interfaces;
using opn_chat.WebAPI.Hubs;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _admin;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminController(IAdminService admin, IHubContext<NotificationHub> hubContext)
        {
            _admin = admin;
            _hubContext = hubContext;
        }

        private Guid AdminId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string AdminNick => User.FindFirst(ClaimTypes.Name)!.Value;

        // ── Stats & Live ────────────────────────────────────────────────────────

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats() => Ok(await _admin.GetStatsAsync());

        [HttpGet("live")]
        public async Task<IActionResult> GetLive() => Ok(await _admin.GetLiveDataAsync());

        // ── Users ───────────────────────────────────────────────────────────────

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
            => Ok(await _admin.GetUsersAsync(page, pageSize, search));

        [HttpPost("users/{id}/ban")]
        public async Task<IActionResult> BanUser(Guid id, [FromBody] BanUserRequestDto dto)
        {
            await _admin.BanUserAsync(AdminId, AdminNick, id, dto);
            return Ok();
        }

        [HttpPost("users/{id}/unban")]
        public async Task<IActionResult> UnbanUser(Guid id)
        {
            await _admin.UnbanUserAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpPost("users/{id}/kick")]
        public async Task<IActionResult> KickUser(Guid id)
        {
            var nick = await _admin.GetUserNicknameAsync(id) ?? id.ToString();
            await _admin.LogKickAsync(AdminId, AdminNick, id, nick);
            await _hubContext.Clients.User(id.ToString()).SendAsync("Kicked");
            return Ok();
        }

        [HttpPost("users/{id}/mute")]
        public async Task<IActionResult> MuteUser(Guid id)
        {
            await _admin.MuteUserAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpPost("users/{id}/unmute")]
        public async Task<IActionResult> UnmuteUser(Guid id)
        {
            await _admin.UnmuteUserAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpPost("users/{id}/force-logout")]
        public async Task<IActionResult> ForceLogout(Guid id)
        {
            await _admin.ForceLogoutAsync(AdminId, AdminNick, id);
            await _hubContext.Clients.User(id.ToString()).SendAsync("Kicked");
            return Ok();
        }

        [HttpPost("users/{id}/toggle-admin")]
        public async Task<IActionResult> ToggleAdmin(Guid id, [FromBody] ToggleAdminRequestDto dto)
        {
            await _admin.ToggleAdminAsync(AdminId, AdminNick, id, dto.IsAdmin);
            return Ok();
        }

        [HttpPost("users/{id}/reset-nickname-changes")]
        public async Task<IActionResult> ResetNicknameChanges(Guid id)
        {
            await _admin.ResetNicknameChangesAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpPost("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            await _admin.DeactivateUserAsync(AdminId, AdminNick, id);
            return Ok();
        }

        // ── Rooms ───────────────────────────────────────────────────────────────

        [HttpGet("rooms")]
        public async Task<IActionResult> GetRooms() => Ok(await _admin.GetRoomsAsync());

        [HttpPost("rooms/{id}/lock")]
        public async Task<IActionResult> LockRoom(Guid id)
        {
            await _admin.LockRoomAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpPost("rooms/{id}/unlock")]
        public async Task<IActionResult> UnlockRoom(Guid id)
        {
            await _admin.UnlockRoomAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpDelete("rooms/{id}")]
        public async Task<IActionResult> DeleteRoom(Guid id)
        {
            await _admin.DeleteRoomAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpDelete("rooms/{id}/messages")]
        public async Task<IActionResult> ClearRoomMessages(Guid id)
        {
            await _admin.ClearRoomMessagesAsync(AdminId, AdminNick, id);
            return Ok();
        }

        // ── Messages ─────────────────────────────────────────────────────────────

        [HttpGet("messages")]
        public async Task<IActionResult> SearchMessages(
            [FromQuery] string? query, [FromQuery] string? userId, [FromQuery] string? roomId,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] bool includeDeleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var p = new AdminMessageSearchParams(query, userId, roomId, from, to, includeDeleted, page, pageSize);
            return Ok(await _admin.SearchMessagesAsync(p));
        }

        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            await _admin.DeleteMessageAsync(AdminId, AdminNick, id);
            return Ok();
        }

        [HttpDelete("messages/user/{userId}")]
        public async Task<IActionResult> BulkDeleteUserMessages(Guid userId)
        {
            await _admin.BulkDeleteUserMessagesAsync(AdminId, AdminNick, userId);
            return Ok();
        }

        // ── Reports ──────────────────────────────────────────────────────────────

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] bool unresolvedOnly = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _admin.GetReportsAsync(unresolvedOnly, page, pageSize));

        [HttpPost("reports/{id}/resolve")]
        public async Task<IActionResult> ResolveReport(Guid id)
        {
            await _admin.ResolveReportAsync(AdminId, AdminNick, id);
            return Ok();
        }

        // ── Audit Logs ───────────────────────────────────────────────────────────

        [HttpGet("auditlogs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] string? action, [FromQuery] string? adminId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var p = new AuditLogParams(from, to, action, adminId, page, pageSize);
            return Ok(await _admin.GetAuditLogsAsync(p));
        }

        // ── Analytics ────────────────────────────────────────────────────────────

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics() => Ok(await _admin.GetAnalyticsAsync());

        // ── Settings ─────────────────────────────────────────────────────────────

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings() => Ok(await _admin.GetSettingsAsync());

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] List<SystemSettingDto> settings)
        {
            await _admin.UpdateSettingsAsync(settings);

            var banner = settings.FirstOrDefault(s => s.Key == "GlobalAnnouncementBanner");
            if (banner != null)
            {
                await _hubContext.Clients.All.SendAsync("AnnouncementBannerUpdated", new { message = banner.Value ?? "" });
            }

            return Ok();
        }

        // ── Command Permissions ───────────────────────────────────────────────────

        [HttpGet("command-permissions")]
        public async Task<IActionResult> GetCommandPermissions()
            => Ok(await _admin.GetCommandPermissionsAsync());

        [HttpPut("command-permissions/{commandName}")]
        public async Task<IActionResult> UpdateCommandPermission(string commandName, [FromBody] UpdateCommandPermissionDto dto)
        {
            await _admin.UpdateCommandPermissionAsync(commandName, dto, AdminId, AdminNick);
            return NoContent();
        }

        [HttpPost("command-permissions/reset")]
        public async Task<IActionResult> ResetCommandPermissions()
        {
            await _admin.ResetCommandPermissionsAsync(AdminId, AdminNick);
            return NoContent();
        }

        // ── Announce ─────────────────────────────────────────────────────────────

        [HttpPost("announce")]
        public async Task<IActionResult> Announce([FromBody] AnnounceRequestDto dto)
        {
            await _admin.LogAnnouncementAsync(AdminId, AdminNick, dto.Message);
            await _hubContext.Clients.All.SendAsync("GlobalAnnouncement", new
            {
                message = dto.Message,
                adminNickname = AdminNick,
                timestamp = DateTime.UtcNow
            });
            return Ok();
        }
    }
}
