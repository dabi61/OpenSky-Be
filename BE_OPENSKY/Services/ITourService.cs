using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface ITourService
    {
        Task<Guid> CreateTourAsync(Guid userId, CreateTourDTO createTourDto);
        Task<bool> UpdateTourAsync(Guid tourId, Guid userId, UpdateTourDTO updateDto);
        Task<bool> SoftDeleteTourAsync(Guid tourId, Guid userId);
        Task<TourResponseDTO?> GetTourByIdAsync(Guid tourId);
        Task<PaginatedToursResponseDTO> GetToursAsync(int page, int size);
        Task<TourSearchResponseDTO> SearchToursAsync(TourSearchDTO searchDto);
        Task<bool> IsTourOwnerAsync(Guid tourId, Guid userId);
    }
}
