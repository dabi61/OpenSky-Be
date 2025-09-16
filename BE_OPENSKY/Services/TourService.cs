using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;
using TourStatus = BE_OPENSKY.Models.TourStatus;

namespace BE_OPENSKY.Services
{
    public class TourService : ITourService
    {
        private readonly ApplicationDbContext _context;

        public TourService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateTourAsync(Guid userId, CreateTourDTO createTourDto)
        {
            var tour = new Tour
            {
                TourID = Guid.NewGuid(),
                UserID = userId,
                TourName = createTourDto.TourName,
                Description = createTourDto.Description,
                Address = createTourDto.Address,
                Province = createTourDto.Province,
                // Star removed from create; will be managed separately
                Price = createTourDto.Price,
                MaxPeople = createTourDto.MaxPeople,
                Status = TourStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return tour.TourID;
        }

        public async Task<bool> UpdateTourAsync(Guid tourId, Guid userId, UpdateTourDTO updateDto)
        {
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == tourId && t.UserID == userId);

            if (tour == null)
                return false;

            // Cập nhật các trường nếu có giá trị
            if (!string.IsNullOrWhiteSpace(updateDto.TourName))
                tour.TourName = updateDto.TourName;

            if (updateDto.Description != null)
                tour.Description = updateDto.Description;

            if (!string.IsNullOrWhiteSpace(updateDto.Address))
                tour.Address = updateDto.Address;

            if (!string.IsNullOrWhiteSpace(updateDto.Province))
                tour.Province = updateDto.Province;

            // Star is no longer updated here
            // if (updateDto.Star.HasValue)
            //     tour.Star = updateDto.Star.Value;

            if (updateDto.Price.HasValue)
                tour.Price = updateDto.Price.Value;

            if (updateDto.MaxPeople.HasValue)
                tour.MaxPeople = updateDto.MaxPeople.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteTourAsync(Guid tourId, Guid userId)
        {
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == tourId && t.UserID == userId);

            if (tour == null)
                return false;

            tour.Status = TourStatus.Removed;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TourResponseDTO?> GetTourByIdAsync(Guid tourId)
        {
            var tour = await _context.Tours
                .Include(t => t.User)
                .Include(t => t.TourItineraries)
                .FirstOrDefaultAsync(t => t.TourID == tourId);

            if (tour == null)
                return null;

            // Lấy ảnh của tour
            var images = await _context.Images
                .Where(i => i.TableType == TableTypeImage.Tour && i.TypeID == tourId)
                .Select(i => i.URL)
                .ToListAsync();

            return new TourResponseDTO
            {
                TourID = tour.TourID,
                UserID = tour.UserID,
                UserName = tour.User.FullName,
                UserEmail = tour.User.Email,
                TourName = tour.TourName,
                Description = tour.Description,
                Address = tour.Address,
                Province = tour.Province,
                Star = tour.Star,
                Price = tour.Price,
                MaxPeople = tour.MaxPeople,
                Status = (TourStatus)tour.Status,
                CreatedAt = tour.CreatedAt,
                Images = images
            };
        }

        public async Task<PaginatedToursResponseDTO> GetToursAsync(int page, int size)
        {
            var query = _context.Tours
                .Include(t => t.User)
                .Where(t => t.Status != TourStatus.Removed)
                .OrderByDescending(t => t.CreatedAt);

            var totalTours = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTours / size);

            var tours = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(t => new TourSummaryDTO
                {
                    TourID = t.TourID,
                    TourName = t.TourName,
                    Address = t.Address,
                    Province = t.Province,
                    Star = t.Star,
                    Price = t.Price,
                    MaxPeople = t.MaxPeople,
                    Status = t.Status,
                    FirstImage = _context.Images
                        .Where(i => i.TableType == TableTypeImage.Tour && i.TypeID == t.TourID)
                        .Select(i => i.URL)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PaginatedToursResponseDTO
            {
                Tours = tours,
                CurrentPage = page,
                PageSize = size,
                TotalTours = totalTours,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<TourSearchResponseDTO> SearchToursAsync(TourSearchDTO searchDto)
        {
            var query = _context.Tours
                .Include(t => t.User)
                .Where(t => t.Status != TourStatus.Removed);

            // Tìm kiếm theo keyword
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                query = query.Where(t => t.TourName.Contains(searchDto.Keyword) ||
                                       (t.Description != null && t.Description.Contains(searchDto.Keyword)));
            }

            // Lọc theo tỉnh thành
            if (!string.IsNullOrWhiteSpace(searchDto.Province))
            {
                query = query.Where(t => t.Province.Contains(searchDto.Province));
            }

            // Lọc theo sao
            if (searchDto.Star.HasValue)
            {
                query = query.Where(t => t.Star == searchDto.Star.Value);
            }

            // Lọc theo giá
            if (searchDto.MinPrice.HasValue)
            {
                query = query.Where(t => t.Price >= searchDto.MinPrice.Value);
            }

            if (searchDto.MaxPrice.HasValue)
            {
                query = query.Where(t => t.Price <= searchDto.MaxPrice.Value);
            }


            // Lọc theo trạng thái
            if (searchDto.Status.HasValue)
            {
                query = query.Where(t => t.Status == searchDto.Status.Value);
            }

            // Sắp xếp
            query = searchDto.SortBy?.ToLower() switch
            {
                "name" => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(t => t.TourName) 
                    : query.OrderByDescending(t => t.TourName),
                "price" => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(t => t.Price) 
                    : query.OrderByDescending(t => t.Price),
                "star" => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(t => t.Star) 
                    : query.OrderByDescending(t => t.Star),
                _ => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(t => t.CreatedAt) 
                    : query.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.Size);

            var tours = await query
                .Skip((searchDto.Page - 1) * searchDto.Size)
                .Take(searchDto.Size)
                .Select(t => new TourSummaryDTO
                {
                    TourID = t.TourID,
                    TourName = t.TourName,
                    Address = t.Address,
                    Province = t.Province,
                    Star = t.Star,
                    Price = t.Price,
                    MaxPeople = t.MaxPeople,
                    Status = t.Status,
                    FirstImage = _context.Images
                        .Where(i => i.TableType == TableTypeImage.Tour && i.TypeID == t.TourID)
                        .Select(i => i.URL)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new TourSearchResponseDTO
            {
                Tours = tours,
                TotalCount = totalCount,
                Page = searchDto.Page,
                Size = searchDto.Size,
                TotalPages = totalPages,
                HasNextPage = searchDto.Page < totalPages,
                HasPreviousPage = searchDto.Page > 1
            };
        }

        public async Task<bool> IsTourOwnerAsync(Guid tourId, Guid userId)
        {
            return await _context.Tours
                .AnyAsync(t => t.TourID == tourId && t.UserID == userId);
        }
    }
}
