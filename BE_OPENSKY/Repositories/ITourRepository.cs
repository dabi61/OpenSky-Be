namespace BE_OPENSKY.Repositories;

public interface ITourRepository
{
    Task<IEnumerable<Tour>> GetAllAsync();
    Task<Tour?> GetByIdAsync(Guid id);
    Task<IEnumerable<Tour>> GetByUserIdAsync(Guid userId);
    Task<Tour> CreateAsync(Tour tour);
    Task<Tour> UpdateAsync(Tour tour);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
