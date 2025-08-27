namespace BE_OPENSKY.Models
{
    // Enum for table types used in Voucher, FeedBack, and Bill
    public enum TableType
    {
        Tour,
        Hotel,
        User,
        Schedule
    }

    // Enum for Hotel status
    public enum HotelStatus
    {
        Draft,
        Active,
        Inactive
    }

    // Enum for Bill status
    public enum BillStatus
    {
        Pending,
        Paid,
        Cancelled,
        Refunded
    }

    // Enum for Room types
    public enum RoomType
    {
        Single = 1,
        Double = 2,
        Triple = 3,
        Suite = 4,
        Deluxe = 5,
        Family = 6
    }

    // Enum for Tour status
    public enum TourStatus
    {
        Draft,
        Active,
        Inactive
    }
}
