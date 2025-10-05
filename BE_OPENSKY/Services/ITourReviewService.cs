using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface ITourReviewService
    {
        Task<Guid> CreateTourReviewAsync(CreateTourReviewDTO createTourReviewDto, Guid userId);
        Task<TourReviewResponseDTO?> GetTourReviewByIdAsync(Guid reviewId);
        Task<List<TourReviewResponseDTO>> GetTourReviewsByTourIdAsync(Guid tourId, int page = 1, int limit = 10);
        Task<bool> UpdateTourReviewAsync(Guid reviewId, UpdateTourReviewDTO updateTourReviewDto, Guid userId);
        Task<bool> DeleteTourReviewAsync(Guid reviewId, Guid userId);
        Task<TourReviewStatsDTO> GetTourReviewStatsAsync(Guid tourId);
        Task<TourReviewEligibilityDTO> CheckReviewEligibilityAsync(Guid tourId, Guid userId);
        Task<PaginatedTourReviewsResponseDTO> GetPaginatedTourReviewsAsync(Guid tourId, int page = 1, int limit = 10);
        Task<List<TourReviewResponseDTO>> GetUserTourReviewsAsync(Guid userId);
    }
}
