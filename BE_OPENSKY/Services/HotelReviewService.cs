using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services;

public class HotelReviewService : IHotelReviewService
{
    private readonly ApplicationDbContext _context;

    public HotelReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HotelReviewResponseDTO?> CreateHotelReviewAsync(Guid hotelId, Guid userId, CreateHotelReviewDTO reviewDto)
    {
        // Kiểm tra Hotel có tồn tại không
        var hotel = await _context.Hotels.FindAsync(hotelId);
        if (hotel == null) return null;

        // Kiểm tra user đã đánh giá Hotel này chưa
        var existingReview = await _context.FeedBacks
            .FirstOrDefaultAsync(f => f.TableType == TableType.Hotel && f.TableID == hotelId && f.UserID == userId);
        
        if (existingReview != null) return null; // User đã đánh giá rồi

        // Tạo review mới
        var feedback = new FeedBack
        {
            FeedBackID = Guid.NewGuid(),
            UserID = userId,
            TableType = TableType.Hotel,
            TableID = hotelId,
            Rate = reviewDto.Rate,
            Description = reviewDto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.FeedBacks.Add(feedback);
        await _context.SaveChangesAsync();

        return await GetHotelReviewByIdAsync(feedback.FeedBackID);
    }

    public async Task<HotelReviewResponseDTO?> UpdateHotelReviewAsync(Guid feedbackId, Guid userId, UpdateHotelReviewDTO updateDto)
    {
        var feedback = await _context.FeedBacks
            .FirstOrDefaultAsync(f => f.FeedBackID == feedbackId && f.UserID == userId && f.TableType == TableType.Hotel);
        
        if (feedback == null) return null;

        feedback.Rate = updateDto.Rate;
        feedback.Description = updateDto.Description;

        await _context.SaveChangesAsync();
        return await GetHotelReviewByIdAsync(feedbackId);
    }

    public async Task<bool> DeleteHotelReviewAsync(Guid feedbackId, Guid userId)
    {
        var feedback = await _context.FeedBacks
            .FirstOrDefaultAsync(f => f.FeedBackID == feedbackId && f.UserID == userId && f.TableType == TableType.Hotel);
        
        if (feedback == null) return false;

        _context.FeedBacks.Remove(feedback);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<HotelReviewResponseDTO?> GetHotelReviewByIdAsync(Guid feedbackId)
    {
        var feedback = await _context.FeedBacks
            .Where(f => f.FeedBackID == feedbackId && f.TableType == TableType.Hotel)
            .Include(f => f.User)
            .FirstOrDefaultAsync();

        if (feedback == null) return null;

        var hotel = await _context.Hotels.FindAsync(feedback.TableID);
        if (hotel == null) return null;

        return new HotelReviewResponseDTO
        {
            FeedBackID = feedback.FeedBackID,
            UserID = feedback.UserID,
            UserName = feedback.User.FullName ?? feedback.User.Email,
            UserAvatar = feedback.User.AvatarURL,
            HotelID = feedback.TableID,
            HotelName = hotel.HotelName,
            Rate = feedback.Rate,
            Description = feedback.Description,
            CreatedAt = feedback.CreatedAt
        };
    }

    public async Task<PaginatedHotelReviewsResponseDTO> GetHotelReviewsAsync(Guid hotelId, int page = 1, int limit = 10)
    {
        var query = _context.FeedBacks
            .Where(f => f.TableType == TableType.Hotel && f.TableID == hotelId)
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync();
        var skip = (page - 1) * limit;
        
        var feedbacks = await query
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        var hotel = await _context.Hotels.FindAsync(hotelId);
        var hotelName = hotel?.HotelName ?? "Unknown Hotel";

        var reviews = feedbacks.Select(f => new HotelReviewResponseDTO
        {
            FeedBackID = f.FeedBackID,
            UserID = f.UserID,
            UserName = f.User.FullName ?? f.User.Email,
            UserAvatar = f.User.AvatarURL,
            HotelID = f.TableID,
            HotelName = hotelName,
            Rate = f.Rate,
            Description = f.Description,
            CreatedAt = f.CreatedAt
        }).ToList();

        var stats = await GetHotelReviewStatsAsync(hotelId);

        return new PaginatedHotelReviewsResponseDTO
        {
            Reviews = reviews,
            TotalCount = totalCount,
            Page = page,
            Limit = limit,
            TotalPages = (int)Math.Ceiling((double)totalCount / limit),
            Stats = stats
        };
    }

    public async Task<HotelReviewStatsDTO> GetHotelReviewStatsAsync(Guid hotelId)
    {
        var reviews = await _context.FeedBacks
            .Where(f => f.TableType == TableType.Hotel && f.TableID == hotelId)
            .ToListAsync();

        if (!reviews.Any())
        {
            return new HotelReviewStatsDTO();
        }

        var totalReviews = reviews.Count;
        var averageRating = reviews.Average(f => f.Rate);
        
        var ratingCounts = reviews.GroupBy(f => f.Rate)
            .ToDictionary(g => g.Key, g => g.Count());

        return new HotelReviewStatsDTO
        {
            AverageRating = Math.Round(averageRating, 1),
            TotalReviews = totalReviews,
            Rating1Count = ratingCounts.GetValueOrDefault(1, 0),
            Rating2Count = ratingCounts.GetValueOrDefault(2, 0),
            Rating3Count = ratingCounts.GetValueOrDefault(3, 0),
            Rating4Count = ratingCounts.GetValueOrDefault(4, 0),
            Rating5Count = ratingCounts.GetValueOrDefault(5, 0)
        };
    }

    public async Task<List<HotelReviewResponseDTO>> GetUserHotelReviewsAsync(Guid userId)
    {
        var feedbacks = await _context.FeedBacks
            .Where(f => f.UserID == userId && f.TableType == TableType.Hotel)
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var hotelIds = feedbacks.Select(f => f.TableID).Distinct().ToList();
        var hotels = await _context.Hotels
            .Where(h => hotelIds.Contains(h.HotelID))
            .ToDictionaryAsync(h => h.HotelID, h => h.HotelName);

        return feedbacks.Select(f => new HotelReviewResponseDTO
        {
            FeedBackID = f.FeedBackID,
            UserID = f.UserID,
            UserName = f.User.FullName ?? f.User.Email,
            UserAvatar = f.User.AvatarURL,
            HotelID = f.TableID,
            HotelName = hotels.GetValueOrDefault(f.TableID, "Unknown Hotel"),
            Rate = f.Rate,
            Description = f.Description,
            CreatedAt = f.CreatedAt
        }).ToList();
    }
}
