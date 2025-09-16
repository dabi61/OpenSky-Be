using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface ITourItineraryService
    {
        Task<Guid> CreateTourItineraryAsync(CreateTourItineraryDTO createTourItineraryDto);
        Task<TourItineraryResponseDTO?> GetTourItineraryByIdAsync(Guid itineraryId);
        Task<List<TourItineraryResponseDTO>> GetTourItinerariesByTourIdAsync(Guid tourId);
        Task<bool> UpdateTourItineraryAsync(Guid itineraryId, UpdateTourItineraryDTO updateTourItineraryDto);
        Task<bool> DeleteTourItineraryAsync(Guid itineraryId);
    }
}
