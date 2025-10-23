using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class TourReviewService : ITourReviewService
    {
        private readonly ApplicationDbContext _context;

        public TourReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateTourReviewAsync(CreateTourReviewDTO createTourReviewDto, Guid userId)
        {
            // Kiểm tra tour có tồn tại không
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == createTourReviewDto.TourId);
            if (tour == null)
                throw new ArgumentException("Tour không tồn tại");

            // Kiểm tra user đã đánh giá tour này chưa
            var existingReview = await _context.FeedBacks
                .FirstOrDefaultAsync(f => f.UserID == userId && 
                                        f.TableType == TableType.Tour && 
                                        f.TableID == createTourReviewDto.TourId);
            if (existingReview != null)
                throw new InvalidOperationException("Bạn đã đánh giá tour này rồi");

            // Kiểm tra user có booking và thanh toán thành công không
            // Chỉ cần có Bill đã thanh toán là đủ (không cần kiểm tra Booking status)
            var hasPaidBill = await _context.Bills
                .AnyAsync(b => b.UserID == userId && 
                              b.Booking != null &&
                              b.Booking.TourID == createTourReviewDto.TourId &&
                              b.Status == BillStatus.Paid);

            if (!hasPaidBill)
                throw new InvalidOperationException("Bạn cần đặt tour và thanh toán thành công trước khi đánh giá");

            var feedback = new FeedBack
            {
                FeedBackID = Guid.NewGuid(),
                UserID = userId,
                TableType = TableType.Tour,
                TableID = createTourReviewDto.TourId,
                Rate = createTourReviewDto.Rate,
                Description = createTourReviewDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.FeedBacks.Add(feedback);
            await _context.SaveChangesAsync();

            // Cập nhật sao tour sau khi tạo feedback
            await UpdateTourRatingAsync(createTourReviewDto.TourId);

            return feedback.FeedBackID;
        }

        public async Task<TourReviewResponseDTO?> GetTourReviewByIdAsync(Guid reviewId)
        {
            var review = await _context.FeedBacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FeedBackID == reviewId && f.TableType == TableType.Tour);

            if (review == null) return null;

            // Lấy thông tin tour
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == review.TableID);

            return new TourReviewResponseDTO
            {
                FeedBackID = review.FeedBackID,
                UserID = review.UserID,
                UserName = review.User?.FullName ?? "Unknown",
                UserAvatar = review.User?.AvatarURL,
                TourID = review.TableID,
                TourName = tour?.TourName ?? "Unknown Tour",
                Rate = review.Rate,
                Description = review.Description,
                CreatedAt = review.CreatedAt
            };
        }

        public async Task<List<TourReviewResponseDTO>> GetTourReviewsByTourIdAsync(Guid tourId, int page = 1, int limit = 10)
        {
            var reviews = await _context.FeedBacks
                .Include(f => f.User)
                .Where(f => f.TableType == TableType.Tour && f.TableID == tourId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            // Lấy thông tin tour
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == tourId);

            return reviews.Select(review => new TourReviewResponseDTO
            {
                FeedBackID = review.FeedBackID,
                UserID = review.UserID,
                UserName = review.User?.FullName ?? "Unknown",
                UserAvatar = review.User?.AvatarURL,
                TourID = review.TableID,
                TourName = tour?.TourName ?? "Unknown Tour",
                Rate = review.Rate,
                Description = review.Description,
                CreatedAt = review.CreatedAt
            }).ToList();
        }

        public async Task<bool> UpdateTourReviewAsync(Guid reviewId, UpdateTourReviewDTO updateTourReviewDto, Guid userId)
        {
            var review = await _context.FeedBacks
                .FirstOrDefaultAsync(f => f.FeedBackID == reviewId && 
                                        f.UserID == userId && 
                                        f.TableType == TableType.Tour);

            if (review == null) return false;

            review.Rate = updateTourReviewDto.Rate;
            review.Description = updateTourReviewDto.Description;

            await _context.SaveChangesAsync();
            
            // Cập nhật sao tour sau khi cập nhật feedback
            await UpdateTourRatingAsync(updateTourReviewDto.TourId);
            
            return true;
        }

        public async Task<bool> DeleteTourReviewAsync(Guid reviewId, Guid userId)
        {
            var review = await _context.FeedBacks
                .FirstOrDefaultAsync(f => f.FeedBackID == reviewId && 
                                        f.UserID == userId && 
                                        f.TableType == TableType.Tour);

            if (review == null) return false;

            var tourId = review.TableID; // Lưu tourId trước khi xóa
            _context.FeedBacks.Remove(review);
            await _context.SaveChangesAsync();
            
            // Cập nhật sao tour sau khi xóa feedback
            await UpdateTourRatingAsync(tourId);
            
            return true;
        }

        public async Task<TourReviewStatsDTO> GetTourReviewStatsAsync(Guid tourId)
        {
            var reviews = await _context.FeedBacks
                .Where(f => f.TableType == TableType.Tour && f.TableID == tourId)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new TourReviewStatsDTO();
            }

            var stats = new TourReviewStatsDTO
            {
                TotalReviews = reviews.Count,
                AverageRating = reviews.Average(r => r.Rate),
                Rating1Count = reviews.Count(r => r.Rate == 1),
                Rating2Count = reviews.Count(r => r.Rate == 2),
                Rating3Count = reviews.Count(r => r.Rate == 3),
                Rating4Count = reviews.Count(r => r.Rate == 4),
                Rating5Count = reviews.Count(r => r.Rate == 5)
            };

            return stats;
        }

        // Method để cập nhật sao tour dựa trên tất cả feedback
        private async Task UpdateTourRatingAsync(Guid tourId)
        {
            var tour = await _context.Tours.FindAsync(tourId);
            if (tour == null) return;

            var reviews = await _context.FeedBacks
                .Where(f => f.TableType == TableType.Tour && f.TableID == tourId)
                .ToListAsync();

            if (reviews.Any())
            {
                // Tính điểm trung bình và làm tròn
                var averageRating = reviews.Average(r => r.Rate);
                tour.Star = (int)Math.Round(averageRating);
            }
            else
            {
                // Nếu không có feedback nào, đặt sao về 0
                tour.Star = 0;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<TourReviewEligibilityDTO> CheckReviewEligibilityAsync(Guid tourId, Guid userId)
        {
            // Kiểm tra tour có tồn tại không
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == tourId);
            if (tour == null)
            {
                return new TourReviewEligibilityDTO
                {
                    CanReview = false,
                    Reason = "Tour không tồn tại"
                };
            }

            // Kiểm tra user đã đánh giá chưa
            var hasExistingReview = await _context.FeedBacks
                .AnyAsync(f => f.UserID == userId && 
                              f.TableType == TableType.Tour && 
                              f.TableID == tourId);

            // Kiểm tra có thanh toán thành công không (đủ điều kiện để đánh giá)
            var hasPaidBill = await _context.Bills
                .AnyAsync(b => b.UserID == userId && 
                              b.Booking != null &&
                              b.Booking.TourID == tourId &&
                              b.Status == BillStatus.Paid);

            // Lấy booking cuối cùng (nếu có)
            var lastBooking = await _context.Bookings
                .Where(b => b.UserID == userId && 
                           b.TourID == tourId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            var canReview = !hasExistingReview && hasPaidBill;
            var reason = !canReview ? 
                (hasExistingReview ? "Bạn đã đánh giá tour này rồi" :
                 !hasPaidBill ? "Bạn cần đặt tour và thanh toán thành công trước khi đánh giá" : "") : "";

            return new TourReviewEligibilityDTO
            {
                CanReview = canReview,
                Reason = reason,
                HasExistingReview = hasExistingReview,
                HasValidBooking = hasPaidBill, // Nếu đã thanh toán thì coi như có booking hợp lệ
                HasPaidBill = hasPaidBill,
                LastBookingDate = lastBooking?.CreatedAt
            };
        }

        public async Task<PaginatedTourReviewsResponseDTO> GetPaginatedTourReviewsAsync(Guid tourId, int page = 1, int limit = 10)
        {
            var query = _context.FeedBacks
                .Include(f => f.User)
                .Where(f => f.TableType == TableType.Tour && f.TableID == tourId);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / limit);

            var reviews = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            // Lấy thông tin tour
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourID == tourId);

            var reviewDtos = reviews.Select(review => new TourReviewResponseDTO
            {
                FeedBackID = review.FeedBackID,
                UserID = review.UserID,
                UserName = review.User?.FullName ?? "Unknown",
                UserAvatar = review.User?.AvatarURL,
                TourID = review.TableID,
                TourName = tour?.TourName ?? "Unknown Tour",
                Rate = review.Rate,
                Description = review.Description,
                CreatedAt = review.CreatedAt
            }).ToList();

            var stats = await GetTourReviewStatsAsync(tourId);

            return new PaginatedTourReviewsResponseDTO
            {
                Reviews = reviewDtos,
                TotalCount = totalCount,
                Page = page,
                Limit = limit,
                TotalPages = totalPages,
                Stats = stats
            };
        }

        public async Task<List<TourReviewResponseDTO>> GetUserTourReviewsAsync(Guid userId)
        {
            var reviews = await _context.FeedBacks
                .Include(f => f.User)
                .Where(f => f.UserID == userId && f.TableType == TableType.Tour)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var result = new List<TourReviewResponseDTO>();

            foreach (var review in reviews)
            {
                // Lấy thông tin tour
                var tour = await _context.Tours
                    .FirstOrDefaultAsync(t => t.TourID == review.TableID);

                result.Add(new TourReviewResponseDTO
                {
                    FeedBackID = review.FeedBackID,
                    UserID = review.UserID,
                    UserName = review.User?.FullName ?? "Unknown",
                    UserAvatar = review.User?.AvatarURL,
                    TourID = review.TableID,
                    TourName = tour?.TourName ?? "Unknown Tour",
                    Rate = review.Rate,
                    Description = review.Description,
                    CreatedAt = review.CreatedAt
                });
            }

            return result;
        }
    }
}
