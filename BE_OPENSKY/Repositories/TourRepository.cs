namespace BE_OPENSKY.Repositories;

public class TourRepository : ITourRepository
{
        private readonly ApplicationDbContext _context;

        public TourRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Tour>> GetAllAsync()
        {
            return await _context.Tours
                .Include(t => t.User)
                .ToListAsync();
        }

        public async Task<Tour?> GetByIdAsync(int id)
        {
            return await _context.Tours
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TourID == id);
        }

        public async Task<IEnumerable<Tour>> GetByUserIdAsync(int userId)
        {
            return await _context.Tours
                .Include(t => t.User)
                .Where(t => t.UserID == userId)
                .ToListAsync();
        }

        public async Task<Tour> CreateAsync(Tour tour)
        {
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return tour;
        }

        public async Task<Tour> UpdateAsync(Tour tour)
        {
            _context.Entry(tour).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return tour;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null)
                return false;

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Tours.AnyAsync(t => t.TourID == id);
        }
    
}
