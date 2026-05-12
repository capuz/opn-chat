using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Interfaces;
using opn_chat.WebAPI.Hubs;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPresenceTracker _presenceTracker;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly IConfiguration _config;

        public ProfileController(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IPresenceTracker presenceTracker,
            IHubContext<PresenceHub> presenceHub,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _presenceTracker = presenceTracker;
            _presenceHub = presenceHub;
            _config = config;
        }

        [HttpPut("nickname")]
        public async Task<IActionResult> UpdateNickname([FromBody] UpdateNicknameDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null) return NotFound();

            var today = DateTime.UtcNow.Date;
            var changesUsedToday = (user.NicknameChangesDate?.Date == today) ? user.NicknameChangesToday : 0;

            var adActive = user.NickAdUnlockedUntil.HasValue && user.NickAdUnlockedUntil.Value > DateTime.UtcNow;
            var dailyLimit = adActive ? 2 : 1;
            if (changesUsedToday >= dailyLimit)
                return BadRequest(new { error = "DAILY_LIMIT", changesLeft = 0 });

            var trimmed = dto.Nickname?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length < 2 || trimmed.Length > 30)
                return BadRequest(new { error = "El nickname debe tener entre 2 y 30 caracteres." });

            user.Nickname = trimmed;
            user.NicknameChangesToday = changesUsedToday + 1;
            user.NicknameChangesDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            return Ok(new
            {
                nickname = user.Nickname,
                changesLeft = 0
            });
        }

        [HttpPost("nick-ad-unlock")]
        public async Task<IActionResult> UnlockNickWithAd()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null) return NotFound();

            var hours = _config.GetValue<double>("NickAd:UnlockHours", 12);
            user.NickAdUnlockedUntil = DateTime.UtcNow.AddHours(hours);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            return Ok(new { unlockedUntil = user.NickAdUnlockedUntil.Value });
        }

        [HttpPut("flag")]
        public async Task<IActionResult> UpdateFlag([FromBody] UpdateFlagDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null) return NotFound();

            var countryCode = dto.ShowFlag
                ? dto.CountryCode?.Trim().ToUpperInvariant()
                : null;

            if (dto.ShowFlag && (string.IsNullOrEmpty(countryCode) || countryCode.Length != 2))
                return BadRequest(new { error = "Invalid country code." });

            user.CountryCode = countryCode;
            user.ShowFlag = dto.ShowFlag;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            _presenceTracker.UpdateFlag(userId, user.CountryCode, user.ShowFlag);

            if (_presenceTracker.IsUserOnline(userId))
            {
                await _presenceHub.Clients.All.SendAsync("UserFlagUpdated", new
                {
                    id = userId,
                    showFlag = user.ShowFlag,
                    countryCode = user.CountryCode
                });
            }

            return Ok(new { showFlag = user.ShowFlag, countryCode = user.CountryCode });
        }

        [HttpGet("/api/settings/announcement")]
        public async Task<IActionResult> GetAnnouncement(
            [FromServices] ISystemSettingRepository settings)
        {
            var message = await settings.GetValueAsync("GlobalAnnouncementBanner");
            return Ok(new { message = message ?? "" });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null) return NotFound();

            return Ok(new
            {
                id = user.Id,
                nickname = user.Nickname,
                email = user.Email,
                nicknameChangesLeft = (user.NicknameChangesDate?.Date == DateTime.UtcNow.Date) ? Math.Max(0, 1 - user.NicknameChangesToday) : 1,
                showFlag = user.ShowFlag,
                countryCode = user.CountryCode,
                globalBadge = user.GlobalBadge,
                createdAt = user.CreatedAt
            });
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] opn_chat.Application.DTOs.UpdatePreferencesDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null) return NotFound();

            var allowed = new[] { "auto", "en", "es", "pt-BR" };
            if (!allowed.Contains(dto.PreferredLanguage)) return BadRequest(new { error = "Invalid language value." });

            user.PreferredLanguage = dto.PreferredLanguage;
            user.Timezone = dto.Timezone;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            return NoContent();
        }

        [HttpPut("admin/badge")]
        public async Task<IActionResult> SetBadge([FromBody] SetBadgeDto dto)
        {
            var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerId)) return Unauthorized();

            var caller = await _userRepository.GetByIdAsync(Guid.Parse(callerId));
            if (caller?.GlobalBadge != "founder") return Forbid();

            var target = await _userRepository.GetByIdAsync(dto.UserId);
            if (target == null) return NotFound();

            var allowed = new[] { "founder", "moderator", null };
            if (!allowed.Contains(dto.Badge)) return BadRequest(new { error = "Invalid badge value." });

            target.GlobalBadge = dto.Badge;
            await _userRepository.UpdateAsync(target);
            await _unitOfWork.CommitAsync();

            return Ok(new { userId = dto.UserId, badge = dto.Badge });
        }
    }

    public record UpdateNicknameDto(string? Nickname);
    public record UpdateFlagDto(bool ShowFlag, string? CountryCode);
    public record SetBadgeDto(Guid UserId, string? Badge);
}
