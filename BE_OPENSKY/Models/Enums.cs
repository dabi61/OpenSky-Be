namespace BE_OPENSKY.Models
{
    // Enum cho các loại bảng được sử dụng trong Voucher, FeedBack và Bill
    public enum TableType
    {
        Tour,
        Hotel
    }

    // Enum cho các loại bảng được sử dụng trong Image
    public enum TableTypeImage
    {
        User,
        Hotel,
        RoomHotel,
        Tour
    }

    // Enum cho trạng thái khách sạn
    public enum HotelStatus
    {
        Active,   // Hoạt động
        Inactive, // Không hoạt động
        Suspend,  // Tạm ngưng
        Removed   // Đã xóa
    }

    // Enum cho trạng thái hóa đơn
    public enum BillStatus
    {
        Pending,   // Chờ xử lý
        Paid,      // Đã thanh toán
        Cancelled, // Đã hủy
        Refunded   // Đã hoàn tiền
    }



    // Enum cho trạng thái tour
    public enum TourStatus
    {
        Active,   // Hoạt động
        Inactive  // Không hoạt động
    }

    // Enum cho trạng thái phòng
    public enum RoomStatus
    {
        Available,    // Có sẵn
        Occupied,     // Đã được đặt
        Maintenance  // Bảo trì
    }

    // Enum cho trạng thái booking
    public enum BookingStatus
    {
        Pending,      // Chờ xác nhận
        Confirmed,    // Đã xác nhận
        Cancelled,    // Đã hủy
        Completed,    // Hoàn thành
        Refunded      // Đã hoàn tiền
    }

    // Enum cho trạng thái người dùng
    public enum UserStatus
    {
        Active,       
        Banned        // Bị cấm
    }

    // Enum cho trạng thái refund
    public enum RefundStatus
    {
        Pending,      // Chờ xử lý
        Approve,      // Đã duyệt
        Deny          // Từ chối
    }
}
