using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(ApplicationDbContext context, ILogger<StatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BillMonthlyStatisticsResponseDTO> GetBillMonthlyStatisticsAsync(int year)
    {
        try
        {
            // Lấy tất cả bills trong năm đó với status = Paid
            var bills = await _context.Bills
                .Where(b => b.CreatedAt.Year == year && b.Status == BillStatus.Paid)
                .Select(b => new { b.CreatedAt.Month, b.TotalPrice })
                .ToListAsync();

            // Group theo tháng và tính tổng
            var monthlyData = bills
                .GroupBy(b => b.Month)
                .Select(g => new BillMonthlyStatisticsDTO
                {
                    Month = g.Key,
                    BillCount = g.Count(),
                    TotalAmount = g.Sum(b => b.TotalPrice)
                })
                .OrderBy(m => m.Month)
                .ToList();

            // Đảm bảo có đủ 12 tháng (tháng nào không có bill thì count = 0, amount = 0)
            var result = new BillMonthlyStatisticsResponseDTO
            {
                Year = year,
                MonthlyData = new List<BillMonthlyStatisticsDTO>()
            };

            for (int month = 1; month <= 12; month++)
            {
                var monthData = monthlyData.FirstOrDefault(m => m.Month == month);
                result.MonthlyData.Add(monthData ?? new BillMonthlyStatisticsDTO
                {
                    Month = month,
                    BillCount = 0,
                    TotalAmount = 0
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bill monthly statistics for year {Year}", year);
            throw;
        }
    }

    public async Task<UserCountByRoleDTO> GetCustomerCountAsync()
    {
        try
        {
            var count = await _context.Users
                .Where(u => u.Role == RoleConstants.Customer && u.Status == UserStatus.Active)
                .CountAsync();

            return new UserCountByRoleDTO
            {
                Role = RoleConstants.Customer,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer count");
            throw;
        }
    }

    public async Task<UserCountByRoleDTO> GetSupervisorCountAsync()
    {
        try
        {
            var count = await _context.Users
                .Where(u => u.Role == RoleConstants.Supervisor && u.Status == UserStatus.Active)
                .CountAsync();

            return new UserCountByRoleDTO
            {
                Role = RoleConstants.Supervisor,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supervisor count");
            throw;
        }
    }

    public async Task<UserCountByRoleDTO> GetTourGuideCountAsync()
    {
        try
        {
            var count = await _context.Users
                .Where(u => u.Role == RoleConstants.TourGuide && u.Status == UserStatus.Active)
                .CountAsync();

            return new UserCountByRoleDTO
            {
                Role = RoleConstants.TourGuide,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tour guide count");
            throw;
        }
    }

    public async Task<HotelCountDTO> GetHotelCountAsync(int? month, int? year)
    {
        try
        {
            IQueryable<Hotel> query = _context.Hotels.Where(h => h.Status == HotelStatus.Active);

            // Nếu có truyền month và year thì filter theo tháng/năm tạo
            if (month.HasValue && year.HasValue)
            {
                query = query.Where(h => h.CreatedAt.Month == month.Value && h.CreatedAt.Year == year.Value);
            }

            var count = await query.CountAsync();

            return new HotelCountDTO
            {
                Month = month,
                Year = year,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel count for month {Month}, year {Year}", month, year);
            throw;
        }
    }

    public async Task<TourCountDTO> GetTourCountAsync(int? month, int? year)
    {
        try
        {
            IQueryable<Tour> query = _context.Tours.Where(t => t.Status == TourStatus.Active);

            // Nếu có truyền month và year thì filter theo tháng/năm tạo
            if (month.HasValue && year.HasValue)
            {
                query = query.Where(t => t.CreatedAt.Month == month.Value && t.CreatedAt.Year == year.Value);
            }

            var count = await query.CountAsync();

            return new TourCountDTO
            {
                Month = month,
                Year = year,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tour count for month {Month}, year {Year}", month, year);
            throw;
        }
    }
}

