using ElasoftCommunityManagementSystem.Dtos;
using ElasoftCommunityManagementSystem.Dtos.EventDtos;
using ElasoftCommunityManagementSystem.Exceptions;
using ElasoftCommunityManagementSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElasoftCommunityManagementSystem.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService;
        }

        // ✅ 1. Etkinlikleri listele
        [HttpGet("listele")]
        public async Task<IActionResult> GetEvents([FromQuery] int? clubId, [FromQuery] string? search)
        {
            // Get user data from token
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role)?.ToLower();

            // Call IEventService with those parameters
            var result = await _eventService.GetEvents(clubId, userId, userRole, search);

            return Ok(result);
        }

        // ✅ 2. Etkinlik oluştur
        [HttpPost("ekle")]
        [Authorize]
        public async Task<IActionResult> CreateEvent([FromForm] CreateEventDto eventDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new { Success = false, Message = "Validation failed", Errors = errors });
                }
                
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userRole = User.FindFirstValue(ClaimTypes.Role)?.ToLower();
                
                // Ensure EventType is set if missing
                
                
                var result = await _eventService.CreateEvent(eventDto, userId, userRole);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (BusinessException ex)
            {
                return StatusCode((int)ex.StatusCode, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the specific exception
                Console.WriteLine($"Error creating event: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { Success = false, Message = "An error occurred while creating the event.", Details = ex.Message });
            }
        }

        // ✅ 3. Güncelle
        [HttpPut("{id}/update")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(int id, [FromForm] CreateEventDto eventDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role)?.ToLower();
            var result = await _eventService.UpdateEvent(id, eventDto, userId, userRole);

            if (!result.Success)
                return Forbid(result.Message);

            return Ok(result);
        }

        // ✅ 4. Sil
        [HttpDelete("{id}/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role)?.ToLower();
            var result = await _eventService.DeleteEvent(id, userId, userRole);

            if (!result.Success)
                return Forbid(result.Message);

            return Ok(result);
        }

        // ✅ 5. Katıl
        [HttpPost("{eventId}/katil")]
        [Authorize]
        public async Task<IActionResult> JoinEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _eventService.JoinEvent(eventId, userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // ✅ 6. Ayrıl
        [HttpDelete("{eventId}/ayril")]
        [Authorize]
        public async Task<IActionResult> LeaveEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _eventService.LeaveEvent(eventId, userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        [HttpGet("yetkili-etkinlikler")]
        [Authorize(Roles = "advisor,leader")]
        public async Task<IActionResult> GetEventsForAuthorizedUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var result = await _eventService.GetEventsForAuthorizedUser(userId, role);
            return Ok(result);
        }
        [HttpPut("{eventId}/approve")]
        [Authorize(Roles = "advisor")]
        public async Task<IActionResult> ApproveEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _eventService.ApproveOrRejectEvent(eventId, userId, "approved");
            return Ok(new { message = "Etkinlik onaylandı." });
        }

        // 🔥 Danışman reddediyor
        [HttpPut("{eventId}/reject")]
        [Authorize(Roles = "advisor")]
        public async Task<IActionResult> RejectEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _eventService.ApproveOrRejectEvent(eventId, userId, "rejected");
            return Ok(new { message = "Etkinlik reddedildi." });
        }

        // Kullanıcının etkinliğe katılım durumunu kontrol et
        [HttpGet("{eventId}/check-participation")]
        [Authorize]
        public async Task<IActionResult> CheckParticipation(int eventId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var isParticipating = await _eventService.IsUserParticipatingInEvent(userId, eventId);
            return Ok(new { isParticipating = isParticipating });
        }
        // ✅ Etkinliğe katılan kullanıcıları getir
        [HttpGet("{eventId}/participants")]
        public async Task<IActionResult> GetEventParticipants(int eventId)
        {
            var participants = await _eventService.GetEventParticipants(eventId);
            return Ok(participants);
        }

    }
}
