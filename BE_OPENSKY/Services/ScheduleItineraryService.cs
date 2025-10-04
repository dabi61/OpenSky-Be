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
                    .ThenInclude(ti => ti.Tour)
                .Include(si => si.Schedule)
                    .ThenInclude(s => s.Tour)
                .Include(si => si.Schedule)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(si => si.ScheduleItID == scheduleItId);

            if (scheduleItinerary == null) return null;

            // Build Schedule DTO
            ScheduleResponseDTO? scheduleDto = null;
            if (scheduleItinerary.Schedule != null)
            {
                var schedule = scheduleItinerary.Schedule;
                var tourImage = await _context.Images
                    .FirstOrDefaultAsync(i => i.TableType == TableTypeImage.Tour && i.TypeID == schedule.TourID);

                scheduleDto = new ScheduleResponseDTO
                {
                    ScheduleID = schedule.ScheduleID,
                    TourID = schedule.TourID,
                    UserID = schedule.UserID,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    NumberPeople = schedule.Tour != null ? Math.Max(0, schedule.Tour.MaxPeople - schedule.CurrentBookings) : 0,
                    Status = schedule.Status,
                    CreatedAt = schedule.CreatedAt,
                    TourName = schedule.Tour?.TourName,
                    User = schedule.User != null ? new UserInfoDTO
                    {
                        UserID = schedule.User.UserID,
                        Email = schedule.User.Email,
                        FullName = schedule.User.FullName,
                        PhoneNumber = schedule.User.PhoneNumber,
                        CitizenId = schedule.User.CitizenId
                    } : null,
                    Tour = schedule.Tour != null ? new ScheduleTourInfoDTO
                    {
                        TourID = schedule.Tour.TourID,
                        TourName = schedule.Tour.TourName,
                        Description = schedule.Tour.Description,
                        MaxPeople = schedule.Tour.MaxPeople,
                        Price = schedule.Tour.Price,
                        Star = schedule.Tour.Star,
                        ImageUrl = tourImage?.URL
                    } : null
                };
            }

            // Build TourItinerary DTO
            TourItineraryResponseDTO? tourItineraryDto = null;
            if (scheduleItinerary.TourItinerary != null)
            {
                var tourItinerary = scheduleItinerary.TourItinerary;
                tourItineraryDto = new TourItineraryResponseDTO
                {
                    ItineraryID = tourItinerary.ItineraryID,
                    TourID = tourItinerary.TourID,
                    DayNumber = tourItinerary.DayNumber,
                    Location = tourItinerary.Location,
                    Description = tourItinerary.Description,
                    IsDeleted = tourItinerary.IsDeleted,
                    TourName = tourItinerary.Tour?.TourName,
                    CreatedAt = tourItinerary.CreatedAt
                };
            }

            return new ScheduleItineraryResponseDTO
            {
                ScheduleItID = scheduleItinerary.ScheduleItID,
                ScheduleID = scheduleItinerary.ScheduleID,
                ItineraryID = scheduleItinerary.ItineraryID,
                StartTime = scheduleItinerary.StartTime,
                EndTime = scheduleItinerary.EndTime,
                Location = scheduleItinerary.TourItinerary?.Location,
                Description = scheduleItinerary.TourItinerary?.Description,
                DayNumber = scheduleItinerary.TourItinerary?.DayNumber ?? 0,
                Schedule = scheduleDto,
                TourItinerary = tourItineraryDto
            };
        }

        public async Task<List<ScheduleItineraryResponseDTO>> GetScheduleItinerariesByScheduleIdAsync(Guid scheduleId)
        {
            var scheduleItineraries = await _context.ScheduleItineraries
                .Include(si => si.TourItinerary)
                    .ThenInclude(ti => ti.Tour)
                .Include(si => si.Schedule)
                    .ThenInclude(s => s.Tour)
                .Include(si => si.Schedule)
                    .ThenInclude(s => s.User)
                .Where(si => si.ScheduleID == scheduleId)
                .OrderBy(si => si.StartTime)
                .ToListAsync();

            // Get tour image once for the schedule
            var schedule = scheduleItineraries.FirstOrDefault()?.Schedule;
            var tourImage = schedule != null ? await _context.Images
                .FirstOrDefaultAsync(i => i.TableType == TableTypeImage.Tour && i.TypeID == schedule.TourID) : null;

            var result = new List<ScheduleItineraryResponseDTO>();
            foreach (var si in scheduleItineraries)
            {
                // Build Schedule DTO (reuse the same schedule for all items)
                ScheduleResponseDTO? scheduleDto = null;
                if (si.Schedule != null)
                {
                    var s = si.Schedule;
                    scheduleDto = new ScheduleResponseDTO
                    {
                        ScheduleID = s.ScheduleID,
                        TourID = s.TourID,
                        UserID = s.UserID,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        NumberPeople = s.Tour != null ? Math.Max(0, s.Tour.MaxPeople - s.CurrentBookings) : 0,
                        Status = s.Status,
                        CreatedAt = s.CreatedAt,
                        TourName = s.Tour?.TourName,
                        User = s.User != null ? new UserInfoDTO
                        {
                            UserID = s.User.UserID,
                            Email = s.User.Email,
                            FullName = s.User.FullName,
                            PhoneNumber = s.User.PhoneNumber,
                            CitizenId = s.User.CitizenId
                        } : null,
                        Tour = s.Tour != null ? new ScheduleTourInfoDTO
                        {
                            TourID = s.Tour.TourID,
                            TourName = s.Tour.TourName,
                            Description = s.Tour.Description,
                            MaxPeople = s.Tour.MaxPeople,
                            Price = s.Tour.Price,
                            Star = s.Tour.Star,
                            ImageUrl = tourImage?.URL
                        } : null
                    };
                }

                // Build TourItinerary DTO
                TourItineraryResponseDTO? tourItineraryDto = null;
                if (si.TourItinerary != null)
                {
                    var ti = si.TourItinerary;
                    tourItineraryDto = new TourItineraryResponseDTO
                    {
                        ItineraryID = ti.ItineraryID,
                        TourID = ti.TourID,
                        DayNumber = ti.DayNumber,
                        Location = ti.Location,
                        Description = ti.Description,
                        IsDeleted = ti.IsDeleted,
                        TourName = ti.Tour?.TourName,
                        CreatedAt = ti.CreatedAt
                    };
                }

                result.Add(new ScheduleItineraryResponseDTO
                {
                    ScheduleItID = si.ScheduleItID,
                    ScheduleID = si.ScheduleID,
                    ItineraryID = si.ItineraryID,
                    StartTime = si.StartTime,
                    EndTime = si.EndTime,
                    Location = si.TourItinerary?.Location,
                    Description = si.TourItinerary?.Description,
                    DayNumber = si.TourItinerary?.DayNumber ?? 0,
                    Schedule = scheduleDto,
                    TourItinerary = tourItineraryDto
                });
            }

            return result;
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
