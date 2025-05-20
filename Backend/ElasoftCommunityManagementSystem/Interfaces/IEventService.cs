﻿using ElasoftCommunityManagementSystem.Dtos.EventDtos;

namespace ElasoftCommunityManagementSystem.Interfaces
{
    public interface IEventService
    {
        Task<List<EventDto>> GetEvents(int? clubId, int userId, string userRole, string? search);
        Task<EventDto> CreateEvent(CreateEventDto eventDto, int userId, string userRole);
        Task<EventDto> UpdateEvent(int id, CreateEventDto eventDto, int userId, string userRole);
        Task<EventDto> DeleteEvent(int id, int userId, string userRole);
        Task<EventDto> JoinEvent(int eventId, int userId);
        Task<EventDto> LeaveEvent(int eventId, int userId);
        Task<List<EventDto>> GetEventsForAuthorizedUser(int userId, string userRole);
        Task ApproveOrRejectEvent(int eventId, int advisorId, string status);
        Task<bool> IsUserParticipatingInEvent(int userId, int eventId);
        Task<List<EventParticipantDetailDto>> GetEventParticipants(int eventId);

    }
}
