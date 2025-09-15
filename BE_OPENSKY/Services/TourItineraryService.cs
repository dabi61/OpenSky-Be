using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class TourItineraryService : ITourItineraryService
    {
        private readonly ApplicationDbContext _context;

        public TourItineraryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateTourItineraryAsync(CreateTourItineraryDTO createTourItineraryDto)
        {
            var tourItinerary = new TourItinerary
            {
                ItineraryID = Guid.NewGuid(),
                TourID = createTourItineraryDto.TourID,
                DayNumber = createTourItineraryDto.DayNumber,
                Location = createTourItineraryDto.Location,
                Description = createTourItineraryDto.Description,
                IsDeleted = false
            };

            _context.TourItineraries.Add(tourItinerary);
            await _context.SaveChangesAsync();

            return tourItinerary.ItineraryID;
        }

        public async Task<TourItineraryResponseDTO?> GetTourItineraryByIdAsync(Guid itineraryId)
        {
            var tourItinerary = await _context.TourItineraries
                .Include(ti => ti.Tour)
                .FirstOrDefaultAsync(ti => ti.ItineraryID == itineraryId && !ti.IsDeleted);

            if (tourItinerary == null) return null;

            return new TourItineraryResponseDTO
            {
                ItineraryID = tourItinerary.ItineraryID,
                TourID = tourItinerary.TourID,
                DayNumber = tourItinerary.DayNumber,
                Location = tourItinerary.Location,
                Description = tourItinerary.Description,
                IsDeleted = tourItinerary.IsDeleted,
                TourName = tourItinerary.Tour?.TourName
            };
        }

        public async Task<List<TourItineraryResponseDTO>> GetTourItinerariesByTourIdAsync(Guid tourId)
        {
            var tourItineraries = await _context.TourItineraries
                .Include(ti => ti.Tour)
                .Where(ti => ti.TourID == tourId && !ti.IsDeleted)
                .OrderBy(ti => ti.DayNumber)
                .Select(ti => new TourItineraryResponseDTO
                {
                    ItineraryID = ti.ItineraryID,
                    TourID = ti.TourID,
                    DayNumber = ti.DayNumber,
                    Location = ti.Location,
                    Description = ti.Description,
                    IsDeleted = ti.IsDeleted,
                    TourName = ti.Tour!.TourName
                })
                .ToListAsync();

            return tourItineraries;
        }

        public async Task<bool> UpdateTourItineraryAsync(Guid itineraryId, UpdateTourItineraryDTO updateTourItineraryDto)
        {
            var tourItinerary = await _context.TourItineraries
                .FirstOrDefaultAsync(ti => ti.ItineraryID == itineraryId && !ti.IsDeleted);

            if (tourItinerary == null) return false;

            if (updateTourItineraryDto.DayNumber.HasValue)
                tourItinerary.DayNumber = updateTourItineraryDto.DayNumber.Value;

            if (!string.IsNullOrEmpty(updateTourItineraryDto.Location))
                tourItinerary.Location = updateTourItineraryDto.Location;

            if (updateTourItineraryDto.Description != null)
                tourItinerary.Description = updateTourItineraryDto.Description;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTourItineraryAsync(Guid itineraryId)
        {
            var tourItinerary = await _context.TourItineraries
                .FirstOrDefaultAsync(ti => ti.ItineraryID == itineraryId && !ti.IsDeleted);

            if (tourItinerary == null) return false;

            tourItinerary.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
