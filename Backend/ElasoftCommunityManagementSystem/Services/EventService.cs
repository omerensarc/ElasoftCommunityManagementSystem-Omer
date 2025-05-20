using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElasoftCommunityManagementSystem.Dtos.EventDtos;
using ElasoftCommunityManagementSystem.Exceptions;
using ElasoftCommunityManagementSystem.Interfaces;
using ElasoftCommunityManagementSystem.Models;
using ElasoftCommunityManagementSystem.Services.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ElasoftCommunityManagementSystem.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly Authorization.EventAuthorizationService _authService;

        public EventService(AppDbContext context, Authorization.EventAuthorizationService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<List<EventDto>> GetEvents(
            int? clubId,
            int userId,
            string userRole,
            string? search
        )
        {
            var query = _context.Event
                .Include(e => e.Club)
                .Include(e => e.EventParticipants)
                .ThenInclude(p => p.User)
                .AsQueryable();

            if (clubId.HasValue)
                query = query.Where(e => e.ClubId == clubId.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(e => e.Name.Contains(search));

            var events = await query.ToListAsync();
            var result = new List<EventDto>();
            var now = DateTime.Now;

            foreach (var ev in events)
            {
                // Aktif katılımcı sayısını hesapla
                var activeParticipantCount = ev.EventParticipants.Count();

                var status =
                    activeParticipantCount >= ev.MaxParticipants ? "Dolu" :
                    now < ev.StartDate && (ev.StartDate - now).TotalDays <= 7 ? "Yaklaşan" :
                    now >= ev.StartDate && now <= ev.EndDate ? "Devam Eden" :
                    now > ev.EndDate ? "Tamamlandı" : "Planlanan";

                var isParticipating = await CheckUserParticipation(userId, ev.EventId);

                var dto = new EventDto
                {
                    EventId = ev.EventId,
                    Name = ev.Name,
                    Description = ev.Description,
                    StartDate = ev.StartDate,
                    EndDate = ev.EndDate,
                    ClubName = ev.Club.Name,
                    MaxParticipants = ev.MaxParticipants,
                    ParticipantCount = activeParticipantCount,
                    ImageUrl = ev.ImageUrl,
                    Status = ev.Status,
                    IsParticipating = isParticipating,
                    Image = ev.Image != null ? Convert.ToBase64String(ev.Image) : null
                };

                var isPresident = await _context.ClubMembership.AnyAsync(m =>
                    m.UserId == userId &&
                    m.ClubId == ev.ClubId &&
                    m.Role.ToLower() == "leader" &&
                    m.Status.ToLower() == "onaylı"
                );

                if (userRole.ToLower() == "admin" || userRole.ToLower() == "advisor" || isPresident)
                {
                    dto.Participants = ev.EventParticipants
                        .Select(p => $"{p.User.Name} {p.User.Surname}")
                        .ToList();
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<EventDto> CreateEvent(CreateEventDto dto, int userId, string userRole)
        {
            if (dto == null)
                throw new ValidationException("Etkinlik verisi boş olamaz.");

            // Kullanıcının etkinlik oluşturma yetkisi var mı kontrol et
            var isAuthorized = await _authService.CanCreateEvent(userId, userRole, dto.ClubId);
            if (!isAuthorized)
                throw new UnauthorizedBusinessException("Bu topluluk için etkinlik oluşturma yetkiniz yok.");

            if (dto.StartDate < DateTime.Now || dto.EndDate <= dto.StartDate)
                throw new ValidationException("Etkinlik tarihleri geçersiz.");

            byte[]? imageData = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await dto.ImageFile.CopyToAsync(ms);
                imageData = ms.ToArray();
            }

            var newEvent = new EventModel
            {
                ClubId = dto.ClubId,
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedAt = DateTime.UtcNow,
                MaxParticipants = dto.MaxParticipants,
                ImageUrl = dto.ImageUrl,
                Image = imageData,
                Status = userRole?.ToLower() == "admin" ? "approved" : "pending",
                ParticipantCount = 0,
            };

            _context.Event.Add(newEvent);
            await _context.SaveChangesAsync();

            return new EventDto
            {
                EventId = newEvent.EventId,
                Name = newEvent.Name,
                Description = newEvent.Description,
                StartDate = newEvent.StartDate,
                EndDate = newEvent.EndDate,
                ClubId = newEvent.ClubId,
                MaxParticipants = newEvent.MaxParticipants,
                ParticipantCount = 0,
                ImageUrl = newEvent.ImageUrl,
                Image = newEvent.Image != null ? Convert.ToBase64String(newEvent.Image) : null,
                Status = newEvent.Status,
                Success = true,
                Message = newEvent.Status == "approved" ? "Etkinlik başarıyla oluşturuldu ve direkt onaylandı." : "Etkinlik başarıyla oluşturuldu ve danışman onayı bekleniyor."
            };
        }

        public async Task<EventDto> UpdateEvent(int id, CreateEventDto dto, int userId, string userRole)
        {
            await _authService.IsUserAuthorizedForEvent(userId, userRole, dto.ClubId);

            var eventEntity = await _context.Event.FindAsync(id);
            if (eventEntity == null)
                throw new ResourceNotFoundException("Etkinlik bulunamadı.");

            if (eventEntity.StartDate < DateTime.Now)
                throw new ValidationException("Geçmiş tarihli etkinlikler güncellenemez.");

            if (!string.IsNullOrEmpty(dto.ImageUrl))
                eventEntity.ImageUrl = dto.ImageUrl;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await dto.ImageFile.CopyToAsync(ms);
                eventEntity.Image = ms.ToArray();
            }

            eventEntity.Name = dto.Name;
            eventEntity.Description = dto.Description;
            eventEntity.StartDate = dto.StartDate;
            eventEntity.EndDate = dto.EndDate;
            eventEntity.MaxParticipants = dto.MaxParticipants;

            await _context.SaveChangesAsync();

            return new EventDto
            {
                EventId = eventEntity.EventId,
                Name = eventEntity.Name,
                Description = eventEntity.Description,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                ClubId = eventEntity.ClubId,
                MaxParticipants = eventEntity.MaxParticipants,
                ParticipantCount = eventEntity.ParticipantCount,
                ImageUrl = eventEntity.ImageUrl,
                Image = eventEntity.Image != null ? Convert.ToBase64String(eventEntity.Image) : null,
                Success = true,
                Message = "Etkinlik başarıyla güncellendi."
            };
        }

        public async Task<EventDto> DeleteEvent(int id, int userId, string userRole)
        {
            await _authService.IsUserAuthorizedForEvent(userId, userRole, id);

            var existingEvent = await _context.Event
                .Include(e => e.Club)
                .FirstOrDefaultAsync(e => e.EventId == id);
            if (existingEvent == null)
                throw new ResourceNotFoundException("Etkinlik bulunamadı.");

            // Geçmiş tarihli etkinlikler silinemiyor
            if (existingEvent.StartDate < DateTime.Now)
                throw new ValidationException("Geçmiş tarihli etkinlikler silinemez.");

            _context.Event.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return new EventDto
            {
                EventId = id,
                Success = true,
                Message = "Etkinlik başarıyla silindi."
            };
        }

        public async Task<EventDto> JoinEvent(int eventId, int userId)
        {
            try
            {
                // Etkinliğe katılma yetkisi kontrolü
                await _authService.CanJoinEvent(userId, eventId);

                var eventEntity = await _context.Event
                    .Include(e => e.EventParticipants)
                    .FirstOrDefaultAsync(e => e.EventId == eventId);

                if (eventEntity == null)
                    throw new ResourceNotFoundException("Etkinlik bulunamadı.");

                var participant = new EventParticipantModel
                {
                    EventId = eventId,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow
                };

                eventEntity.EventParticipants.Add(participant);
                await _context.SaveChangesAsync();

                // Güncel katılımcı sayısını hesapla
                var activeParticipantCount = eventEntity.EventParticipants.Count();

                return new EventDto
                {
                    EventId = eventEntity.EventId,
                    Success = true,
                    Message = "Etkinliğe başarıyla katıldınız.",
                    IsParticipating = true,
                    ParticipantCount = activeParticipantCount
                };
            }
            catch (Exception ex)
            {
                return new EventDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<EventDto> LeaveEvent(int eventId, int userId)
        {
            try
            {
                // Etkinlikten ayrılma yetkisi kontrolü
                await _authService.CanLeaveEvent(userId, eventId);

                var eventEntity = await _context.Event
                    .Include(e => e.EventParticipants)
                    .FirstOrDefaultAsync(e => e.EventId == eventId);

                if (eventEntity == null)
                    throw new ResourceNotFoundException("Etkinlik bulunamadı.");

                var participant = eventEntity.EventParticipants.FirstOrDefault(p => p.UserId == userId);
                if (participant == null)
                    throw new ResourceNotFoundException("Bu etkinliğe katılım kaydınız bulunmuyor.");

                eventEntity.EventParticipants.Remove(participant);
                await _context.SaveChangesAsync();

                // Güncel katılımcı sayısını hesapla
                var activeParticipantCount = eventEntity.EventParticipants.Count();

                return new EventDto
                {
                    EventId = eventEntity.EventId,
                    Success = true,
                    Message = "Etkinlikten başarıyla ayrıldınız.",
                    IsParticipating = false,
                    ParticipantCount = activeParticipantCount
                };
            }
            catch (Exception ex)
            {
                return new EventDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<List<EventDto>> GetEventsForAuthorizedUser(int userId, string userRole)
        {
            // Sadece advisor veya leader girebilir
            var validRoles = new[] { "advisor", "leader" };
            if (!validRoles.Contains(userRole.ToLower()))
                throw new UnauthorizedBusinessException("Bu işlem yalnızca advisor veya leader kullanıcılar içindir.");

            // Kullanıcının advisor veya başkan olduğu kulüp Id'leri
            var clubIds = await _context.ClubMembership
                .Where(m => m.UserId == userId &&
                            (m.Role.ToLower() == "advisor" || m.Role.ToLower() == "leader") &&
                            m.Status.ToLower() == "approved")
                .Select(m => m.ClubId)
                .Distinct()
                .ToListAsync();

            // Kulüp yoksa direkt boş dön
            if (!clubIds.Any())
                return new List<EventDto>();

            // Bu kulüplerin etkinliklerini getir
            var events = await _context.Event
                .Include(e => e.Club)
                .Include(e => e.EventParticipants).ThenInclude(p => p.User)
                .Where(e => clubIds.Contains(e.ClubId))
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            var now = DateTime.Now;
            var result = new List<EventDto>();

            foreach (var ev in events)
            {
                var status =
                    ev.ParticipantCount >= ev.MaxParticipants ? "Dolu" :
                    now < ev.StartDate && (ev.StartDate - now).TotalDays <= 7 ? "Yaklaşan" :
                    now >= ev.StartDate && now <= ev.EndDate ? "Devam Eden" :
                    now > ev.EndDate ? "Tamamlandı" : "Planlanan";

                result.Add(new EventDto
                {
                    EventId = ev.EventId,
                    Name = ev.Name,
                    Description = ev.Description,
                    StartDate = ev.StartDate,
                    EndDate = ev.EndDate,
                    ClubId = ev.ClubId,
                    ClubName = ev.Club.Name,
                    MaxParticipants = ev.MaxParticipants,
                    ParticipantCount = ev.ParticipantCount,
                    ImageUrl = ev.ImageUrl,
                    Status = ev.Status,
                    // Image özelliğini base64 formatında ekleyelim
                    Image = ev.Image != null ? Convert.ToBase64String(ev.Image) : null,
                    Participants = ev.EventParticipants
                        .Select(p => $"{p.User.Name} {p.User.Surname}")
                        .ToList()
                });
            }

            return result;
        }
        public async Task ApproveOrRejectEvent(int eventId, int advisorId, string status)
        {
            // Etkinliği bul
            var eventEntity = await _context.Event.Include(e => e.Club).FirstOrDefaultAsync(e => e.EventId == eventId);
            if (eventEntity == null)
                throw new ResourceNotFoundException("Etkinlik bulunamadı.");

            // Danışman bu kulüpte yetkili mi?
            var isAdvisor = await _context.ClubMembership.AnyAsync(cm =>
                cm.UserId == advisorId &&
                cm.ClubId == eventEntity.ClubId &&
                cm.Role.ToLower() == "advisor" &&
                cm.Status.ToLower() == "approved");

            if (!isAdvisor)
                throw new UnauthorizedBusinessException("Bu etkinliği onaylama yetkiniz yok.");

            // Geçerli durum kontrolü
            status = status.ToLower();
            if (status != "approved" && status != "rejected")
                throw new ValidationException("Geçersiz durum.");

            // Durum değişikliği kontrolü (gereksiz save'i engelle)
            if (eventEntity.Status?.ToLower() == status)
                throw new BusinessException($"Etkinlik zaten {status} durumunda.");

            // Güncelleme
            eventEntity.Status = status;
            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Tarihi dakikaya indirger
        private DateTime TrimToMinute(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
        }

        public async Task<bool> IsUserParticipatingInEvent(int userId, int eventId)
        {
            return await _context.EventParticipant
                .AnyAsync(ep => ep.UserId == userId && ep.EventId == eventId);
        }

        // Etkinlik listesi dönerken katılım durumunu da ekleyelim
        private async Task<bool> CheckUserParticipation(int userId, int eventId)
        {
            return await _context.EventParticipant
                .AnyAsync(ep => ep.UserId == userId && ep.EventId == eventId);
        }
        public async Task<List<EventParticipantDetailDto>> GetEventParticipants(int eventId)
        {
            var participants = await _context.EventParticipant
                .Where(ep => ep.EventId == eventId)
                .Join(
                    _context.Users,
                    ep => ep.UserId,
                    u => u.UserId,
                    (ep, u) => new EventParticipantDetailDto
                    {
                        UserId = u.UserId,
                        FullName = $"{u.Name} {u.Surname}",
                        RegisteredAt = ep.RegisteredAt
                    }
                )
                .ToListAsync();

            return participants;
        }

    }
}
