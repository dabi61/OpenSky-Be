using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services;

public interface IStatisticsService
{
    /// <summary>
    /// Lấy thống kê bill theo tháng từ tháng 1 đến tháng 12
    /// </summary>
    /// <param name="year">Năm cần thống kê</param>
    Task<BillMonthlyStatisticsResponseDTO> GetBillMonthlyStatisticsAsync(int year);
    
    /// <summary>
    /// Lấy số lượng user là Customer
    /// </summary>
    Task<UserCountByRoleDTO> GetCustomerCountAsync();
    
    /// <summary>
    /// Lấy số lượng user là Supervisor
    /// </summary>
    Task<UserCountByRoleDTO> GetSupervisorCountAsync();
    
    /// <summary>
    /// Lấy số lượng user là TourGuide
    /// </summary>
    Task<UserCountByRoleDTO> GetTourGuideCountAsync();
    
    /// <summary>
    /// Lấy số lượng hotel theo tháng (nếu không truyền tháng thì trả về tổng số hotel)
    /// </summary>
    /// <param name="month">Tháng (1-12), null để lấy tất cả</param>
    /// <param name="year">Năm</param>
    Task<HotelCountDTO> GetHotelCountAsync(int? month, int? year);
    
    /// <summary>
    /// Lấy số lượng tour theo tháng (nếu không truyền tháng thì trả về tổng số tour)
    /// </summary>
    /// <param name="month">Tháng (1-12), null để lấy tất cả</param>
    /// <param name="year">Năm</param>
    Task<TourCountDTO> GetTourCountAsync(int? month, int? year);
}

