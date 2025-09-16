using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class ScheduleItineraryService : IScheduleItineraryService
    {
        private readonly ApplicationDbContext _context;

        public ScheduleItineraryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateScheduleItineraryAsync(CreateScheduleItineraryDTO createScheduleItineraryDto)
        {
            var scheduleItinerary = new ScheduleItinerary
            {
                ScheduleItID = Guid.NewGuid(),
                ScheduleID = createScheduleItineraryDto.ScheduleID,
                ItineraryID = createScheduleItineraryDto.ItineraryID,
                StartTime = createScheduleItineraryDto.StartTime,
                EndTime = createScheduleItineraryDto.EndTime
            };

            _context.ScheduleItineraries.Add(scheduleItinerary);
            await _context.SaveChangesAsync();

            return scheduleItinerary.ScheduleItID;
        }

        public async Task<ScheduleItineraryResponseDTO?> GetScheduleItineraryByIdAsync(Guid scheduleItId)
        {
            var scheduleItinerary = await _context.ScheduleItineraries
                .Include(si => si.TourItinerary)
                .FirstOrDefaultAsync(si => si.ScheduleItID == scheduleItId);

            if (scheduleItinerary == null) return null;

            return new ScheduleItineraryResponseDTO
            {
                ScheduleItID = scheduleItinerary.ScheduleItID,
                ScheduleID = scheduleItinerary.ScheduleID,
                ItineraryID = scheduleItinerary.ItineraryID,
                StartTime = scheduleItinerary.StartTime,
                EndTime = scheduleItinerary.EndTime,
                Location = scheduleItinerary.TourItinerary?.Location,
                Description = scheduleItinerary.TourItinerary?.Description,
                DayNumber = scheduleItinerary.TourItinerary?.DayNumber ?? 0
            };
        }

        public async Task<List<ScheduleItineraryResponseDTO>> GetScheduleItinerariesByScheduleIdAsync(Guid scheduleId)
        {
            var scheduleItineraries = await _context.ScheduleItineraries
                .Include(si => si.TourItinerary)
                .Where(si => si.ScheduleID == scheduleId)
                .OrderBy(si => si.StartTime)
                .Select(si => new ScheduleItineraryResponseDTO
                {
                    ScheduleItID = si.ScheduleItID,
                    ScheduleID = si.ScheduleID,
                    ItineraryID = si.ItineraryID,
                    StartTime = si.StartTime,
                    EndTime = si.EndTime,
                    Location = si.TourItinerary!.Location,
                    Description = si.TourItinerary.Description,
                    DayNumber = si.TourItinerary.DayNumber
                })
                .ToListAsync();

            return scheduleItineraries;
        }

        public async Task<bool> UpdateScheduleItineraryAsync(Guid scheduleItId, UpdateScheduleItineraryDTO updateScheduleItineraryDto)
        {
            var scheduleItinerary = await _context.ScheduleItineraries
                .FirstOrDefaultAsync(si => si.ScheduleItID == scheduleItId);

            if (scheduleItinerary == null) return false;

            if (updateScheduleItineraryDto.StartTime.HasValue)
                scheduleItinerary.StartTime = updateScheduleItineraryDto.StartTime.Value;

            if (updateScheduleItineraryDto.EndTime.HasValue)
                scheduleItinerary.EndTime = updateScheduleItineraryDto.EndTime.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteScheduleItineraryAsync(Guid scheduleItId)
        {
            var scheduleItinerary = await _context.ScheduleItineraries
                .FirstOrDefaultAsync(si => si.ScheduleItID == scheduleItId);

            if (scheduleItinerary == null) return false;

            _context.ScheduleItineraries.Remove(scheduleItinerary);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
