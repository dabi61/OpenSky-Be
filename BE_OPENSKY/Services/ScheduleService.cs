using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;
using ScheduleStatus = BE_OPENSKY.Models.ScheduleStatus;

namespace BE_OPENSKY.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly ApplicationDbContext _context;

        public ScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateScheduleAsync(Guid userId, CreateScheduleDTO createScheduleDto)
        {
            var schedule = new Schedule
            {
                ScheduleID = Guid.NewGuid(),
                TourID = createScheduleDto.TourID,
                UserID = createScheduleDto.TourGuideID, // Sử dụng TourGuideID được phân công
                StartTime = createScheduleDto.StartTime,
                EndTime = createScheduleDto.EndTime,
                NumberPeople = createScheduleDto.NumberPeople,
                Status = ScheduleStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return schedule.ScheduleID;
        }

        public async Task<ScheduleResponseDTO?> GetScheduleByIdAsync(Guid scheduleId)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Tour)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId && s.Status != ScheduleStatus.Removed);

            if (schedule == null) return null;

            return new ScheduleResponseDTO
            {
                ScheduleID = schedule.ScheduleID,
                TourID = schedule.TourID,
                UserID = schedule.UserID,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                NumberPeople = schedule.NumberPeople,
                Status = schedule.Status,
                CreatedAt = schedule.CreatedAt,
                TourName = schedule.Tour?.TourName,
                UserName = schedule.User?.FullName
            };
        }

        public async Task<ScheduleListResponseDTO> GetSchedulesAsync(int page = 1, int size = 10)
        {
            var query = _context.Schedules
                .Include(s => s.Tour)
                .Include(s => s.User)
                .Where(s => s.Status != ScheduleStatus.Removed)
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var schedules = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(s => new ScheduleResponseDTO
                {
                    ScheduleID = s.ScheduleID,
                    TourID = s.TourID,
                    UserID = s.UserID,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    NumberPeople = s.NumberPeople,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    TourName = s.Tour!.TourName,
                    UserName = s.User!.FullName,
                    RemainingSlots = s.NumberPeople - s.CurrentBookings
                })
                .ToListAsync();

            return new ScheduleListResponseDTO
            {
                Schedules = schedules,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<ScheduleListResponseDTO> GetSchedulesByTourIdAsync(Guid tourId, int page = 1, int size = 10)
        {
            var query = _context.Schedules
                .Include(s => s.Tour)
                .Include(s => s.User)
                .Where(s => s.TourID == tourId && s.Status != ScheduleStatus.Removed)
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var schedules = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(s => new ScheduleResponseDTO
                {
                    ScheduleID = s.ScheduleID,
                    TourID = s.TourID,
                    UserID = s.UserID,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    NumberPeople = s.NumberPeople,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    TourName = s.Tour!.TourName,
                    UserName = s.User!.FullName,
                    RemainingSlots = s.NumberPeople - s.CurrentBookings
                })
                .ToListAsync();

            return new ScheduleListResponseDTO
            {
                Schedules = schedules,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<bool> UpdateScheduleAsync(Guid scheduleId, UpdateScheduleDTO updateScheduleDto)
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId && s.Status != ScheduleStatus.Removed);

            if (schedule == null) return false;

            if (updateScheduleDto.StartTime.HasValue)
                schedule.StartTime = updateScheduleDto.StartTime.Value;

            if (updateScheduleDto.EndTime.HasValue)
                schedule.EndTime = updateScheduleDto.EndTime.Value;

            if (updateScheduleDto.Status.HasValue)
                schedule.Status = updateScheduleDto.Status.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteScheduleAsync(Guid scheduleId)
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId && s.Status != ScheduleStatus.Removed);

            if (schedule == null) return false;

            schedule.Status = ScheduleStatus.Removed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ScheduleListResponseDTO> GetSchedulesByTourGuideIdAsync(Guid tourGuideId, int page = 1, int size = 10)
        {
            var query = _context.Schedules
                .Include(s => s.Tour)
                .Include(s => s.User)
                .Where(s => s.UserID == tourGuideId && s.Status != ScheduleStatus.Removed)
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var schedules = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(s => new ScheduleResponseDTO
                {
                    ScheduleID = s.ScheduleID,
                    TourID = s.TourID,
                    UserID = s.UserID,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    NumberPeople = s.NumberPeople,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    TourName = s.Tour!.TourName,
                    UserName = s.User!.FullName,
                    RemainingSlots = s.NumberPeople - s.CurrentBookings
                })
                .ToListAsync();

            return new ScheduleListResponseDTO
            {
                Schedules = schedules,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<bool> IsScheduleAssignedToTourGuideAsync(Guid scheduleId, Guid tourGuideId)
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId && s.UserID == tourGuideId && s.Status != ScheduleStatus.Removed);

            return schedule != null;
        }

        public async Task<ScheduleListResponseDTO> GetBookableSchedulesForTourAsync(Guid tourId, DateTime fromDate, DateTime toDate, int guests)
        {
            if (fromDate > toDate)
                (fromDate, toDate) = (toDate, fromDate);

            // Ensure UTC kind for PostgreSQL 'timestamp with time zone'
            if (fromDate.Kind != DateTimeKind.Utc)
                fromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            if (toDate.Kind != DateTimeKind.Utc)
                toDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc);

            var query = _context.Schedules
                .Include(s => s.Tour)
                .Include(s => s.User)
                .Where(s => s.TourID == tourId
                            && s.Status == ScheduleStatus.Active
                            && s.StartTime >= fromDate
                            && s.EndTime <= toDate
                            && (s.NumberPeople - s.CurrentBookings) >= guests)
                .OrderBy(s => s.StartTime);

            var schedules = await query
                .Select(s => new ScheduleResponseDTO
                {
                    ScheduleID = s.ScheduleID,
                    TourID = s.TourID,
                    UserID = s.UserID,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    NumberPeople = s.NumberPeople,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    TourName = s.Tour!.TourName,
                    UserName = s.User!.FullName,
                    RemainingSlots = s.NumberPeople - s.CurrentBookings
                })
                .ToListAsync();

            return new ScheduleListResponseDTO
            {
                Schedules = schedules,
                TotalCount = schedules.Count,
                Page = 1,
                Size = schedules.Count,
                TotalPages = 1
            };
        }
    }
}
