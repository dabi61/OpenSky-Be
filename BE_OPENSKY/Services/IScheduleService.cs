using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IScheduleService
    {
        Task<Guid> CreateScheduleAsync(Guid userId, CreateScheduleDTO createScheduleDto);
        Task<ScheduleResponseDTO?> GetScheduleByIdAsync(Guid scheduleId);
        Task<ScheduleListResponseDTO> GetSchedulesAsync(int page = 1, int size = 10);
        Task<ScheduleListResponseDTO> GetSchedulesByTourIdAsync(Guid tourId, int page = 1, int size = 10);
        Task<ScheduleListResponseDTO> GetSchedulesByTourGuideIdAsync(Guid tourGuideId, int page = 1, int size = 10);
        Task<bool> UpdateScheduleAsync(Guid scheduleId, UpdateScheduleDTO updateScheduleDto);
        Task<bool> SoftDeleteScheduleAsync(Guid scheduleId);
        Task<bool> IsScheduleAssignedToTourGuideAsync(Guid scheduleId, Guid tourGuideId);
        Task<ScheduleListResponseDTO> GetBookableSchedulesForTourAsync(Guid tourId, DateTime fromDate, DateTime toDate, int guests);
    }
}
