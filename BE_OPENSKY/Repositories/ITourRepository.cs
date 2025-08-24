namespace BE_OPENSKY.Repositories;

public interface ITourRepository
{
    Task<IEnumerable<Tour>> GetAllAsync();
    Task<Tour?> GetByIdAsync(int id);
    Task<IEnumerable<Tour>> GetByUserIdAsync(int userId);
    Task<Tour> CreateAsync(Tour tour);
    Task<Tour> UpdateAsync(Tour tour);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
