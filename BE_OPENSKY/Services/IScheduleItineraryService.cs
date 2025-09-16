using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IScheduleItineraryService
    {
        Task<Guid> CreateScheduleItineraryAsync(CreateScheduleItineraryDTO createScheduleItineraryDto);
        Task<ScheduleItineraryResponseDTO?> GetScheduleItineraryByIdAsync(Guid scheduleItId);
        Task<List<ScheduleItineraryResponseDTO>> GetScheduleItinerariesByScheduleIdAsync(Guid scheduleId);
        Task<bool> UpdateScheduleItineraryAsync(Guid scheduleItId, UpdateScheduleItineraryDTO updateScheduleItineraryDto);
        Task<bool> DeleteScheduleItineraryAsync(Guid scheduleItId);
    }
}
