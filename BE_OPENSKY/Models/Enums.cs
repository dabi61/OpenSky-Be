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
        Inactive  // Không hoạt động
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
}
