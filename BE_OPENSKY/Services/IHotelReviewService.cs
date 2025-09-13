namespace BE_OPENSKY.Services;

public interface IHotelReviewService
{
    // Tạo đánh giá Hotel
    Task<HotelReviewResponseDTO?> CreateHotelReviewAsync(Guid hotelId, Guid userId, CreateHotelReviewDTO reviewDto);
    
    // Cập nhật đánh giá Hotel
    Task<HotelReviewResponseDTO?> UpdateHotelReviewAsync(Guid feedbackId, Guid userId, UpdateHotelReviewDTO updateDto);
    
    // Xóa đánh giá Hotel
    Task<bool> DeleteHotelReviewAsync(Guid feedbackId, Guid userId);
    
    // Lấy đánh giá theo ID
    Task<HotelReviewResponseDTO?> GetHotelReviewByIdAsync(Guid feedbackId);
    
    // Lấy danh sách đánh giá Hotel có phân trang
    Task<PaginatedHotelReviewsResponseDTO> GetHotelReviewsAsync(Guid hotelId, int page = 1, int limit = 10);
    
    // Lấy thống kê đánh giá Hotel
    Task<HotelReviewStatsDTO> GetHotelReviewStatsAsync(Guid hotelId);
    
    // Lấy đánh giá Hotel của user
    Task<List<HotelReviewResponseDTO>> GetUserHotelReviewsAsync(Guid userId);
    
    // Kiểm tra điều kiện đánh giá
    Task<ReviewEligibilityDTO> CheckReviewEligibilityAsync(Guid hotelId, Guid userId);
}
