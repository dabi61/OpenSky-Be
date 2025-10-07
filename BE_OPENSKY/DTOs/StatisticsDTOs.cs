namespace BE_OPENSKY.DTOs;

// DTO cho thống kê bill theo tháng
public class BillMonthlyStatisticsDTO
{
    public int Month { get; set; }
    public int BillCount { get; set; }
    public decimal TotalAmount { get; set; }
}

// DTO cho response danh sách thống kê bill theo tháng
public class BillMonthlyStatisticsResponseDTO
{
    public int Year { get; set; }
    public List<BillMonthlyStatisticsDTO> MonthlyData { get; set; } = new();
}

// DTO cho số lượng user theo role
public class UserCountByRoleDTO
{
    public string Role { get; set; } = string.Empty;
    public int Count { get; set; }
}

// DTO cho thống kê hotel theo tháng
public class HotelCountDTO
{
    public int? Month { get; set; } // null nghĩa là thống kê tất cả
    public int? Year { get; set; }
    public int Count { get; set; }
}

// DTO cho thống kê tour theo tháng
public class TourCountDTO
{
    public int? Month { get; set; } // null nghĩa là thống kê tất cả
    public int? Year { get; set; }
    public int Count { get; set; }
}

