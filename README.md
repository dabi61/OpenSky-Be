# BE_OPENSKY - Tài liệu API

Hệ thống quản lý du lịch tích hợp với 16 bảng database và API đầy đủ.

## Database Schema

```mermaid
erDiagram
    User {
        Guid UserID PK
        string Email UK
        string Password
        string FullName
        string Role
        string PhoneNumber
        string CitizenId
        string AvatarURL
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    Session {
        Guid SessionID PK
        Guid UserID FK
        string RefreshToken UK
        DateTime ExpiresAt
        bool IsActive
        DateTime CreatedAt
    }

    Tour {
        Guid TourID PK
        Guid UserID FK
        string Name
        string Address
        string Description
        string Status
        int Star
        decimal Price
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    Hotel {
        Guid HotelID PK
        Guid UserID FK
        string Email
        string Address
        string District
        string Coordinates
        string HotelName
        string Description
        string Status
        int Star
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    HotelRoom {
        Guid RoomID PK
        Guid HotelID FK
        string RoomName
        int RoomType
        string Address
        decimal Price
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    Message {
        Guid MessageID PK
        Guid Sender FK
        Guid Receiver FK
        string MessageText
        bool IsReaded
        DateTime CreatedAt
    }

    Bill {
        Guid BillID PK
        Guid UserID FK
        Guid UserVoucherID FK
        string TableType
        string Status
        decimal Deposit
        decimal RefundPrice
        decimal TotalPrice
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    BillDetail {
        Guid BillDetailID PK
        Guid BillID FK
        Guid ItemID
        string ItemType
        string ItemName
        int Quantity
        decimal UnitPrice
        decimal TotalPrice
        string Notes
    }

    Schedule {
        Guid ScheduleID PK
        Guid TourID FK
        Guid UserID FK
        DateTime StartTime
        DateTime EndTime
        int NumberPeople
        DateTime CreatedAt
    }

    TourItinerary {
        Guid ItineraryID PK
        Guid TourID FK
        string Location
        string Description
        int DayNumber
    }

    ScheduleItinerary {
        Guid ScheduleItID PK
        Guid ScheduleID FK
        Guid ItineraryID FK
        DateTime StartTime
        DateTime EndTime
    }

    FeedBack {
        Guid FeedBackID PK
        Guid UserID FK
        Guid TypeID
        string TableType
        string Rate
        string Description
        DateTime CreatedAt
    }

    Refund {
        Guid RefundID PK
        Guid BillID FK
        string Description
        DateTime CreatedAt
    }

    Notification {
        Guid NotificationID PK
        Guid BillID FK
        string Description
        DateTime CreatedAt
    }

    Image {
        Guid ImgID PK
        Guid TypeID
        string TableType
        string URL
        DateTime CreatedAt
    }

    Voucher {
        Guid VoucherID PK
        string Code UK
        int Percent
        string TableType
        DateTime StartDate
        DateTime EndDate
        string Description
        int MaxUsage
        DateTime CreatedAt
    }

    UserVoucher {
        Guid UserVoucherID PK
        Guid UserID FK
        Guid VoucherID FK
        bool IsUsed
        DateTime SavedAt
    }

    User ||--o{ Session : "has many"
    User ||--o{ Tour : "creates"
    User ||--o{ Hotel : "manages"
    User ||--o{ Message : "sends"
    User ||--o{ Message : "receives"
    User ||--o{ Bill : "makes"
    User ||--o{ Schedule : "books"
    User ||--o{ FeedBack : "gives"
    User ||--o{ UserVoucher : "saves"

    Hotel ||--o{ HotelRoom : "contains"

    Tour ||--o{ Schedule : "scheduled for"
    Tour ||--o{ TourItinerary : "has itinerary"

    Bill ||--o{ BillDetail : "contains"
    Bill ||--o{ Refund : "may have"
    Bill ||--o{ Notification : "generates"
    Bill }o--|| UserVoucher : "uses voucher"

    Schedule ||--o{ ScheduleItinerary : "has detailed itinerary"

    TourItinerary ||--o{ ScheduleItinerary : "scheduled in"

    Voucher ||--o{ UserVoucher : "saved by users"
```

Ghi chú: PK = Primary Key, FK = Foreign Key, UK = Unique Key

## Authentication (/api/auth)

- POST /register: Đăng ký tài khoản mới
- POST /login: Đăng nhập, trả về JWT token
- POST /change-password: Đổi mật khẩu (yêu cầu đăng nhập)
- POST /refresh-token: Làm mới access token
- POST /logout: Đăng xuất, vô hiệu hóa session

## Users (/api/users)

- GET /: Lấy danh sách người dùng (Management)
- GET /{id}: Lấy người dùng theo ID (Management)
- PUT /{id}: Cập nhật thông tin người dùng (chủ sở hữu/Management)
- DELETE /{id}: Xóa người dùng (Admin)
- GET /profile: Lấy thông tin profile hiện tại (đăng nhập)
- PUT /profile: Cập nhật profile (đăng nhập)

## Tours (/api/tours)

- GET /: Lấy tất cả tour (có filter, paging)
- GET /{id}: Lấy tour theo ID
- GET /user/{userId}: Lấy các tour của một user
- POST /: Tạo tour mới (yêu cầu đăng nhập)
- PUT /{id}: Cập nhật tour (chủ sở hữu/Management)
- DELETE /{id}: Xóa tour (chủ sở hữu/Admin)
- GET /{id}/itinerary: Lấy lịch trình tour
- POST /{id}/itinerary: Thêm lịch trình tour (chủ sở hữu/Management)
- PUT /itinerary/{itineraryId}: Cập nhật lịch trình tour (chủ sở hữu/Management)
- DELETE /itinerary/{itineraryId}: Xóa lịch trình tour (chủ sở hữu/Management)

## Hotels (/api/hotels)

- GET /: Lấy tất cả khách sạn (có filter, paging)
- GET /{id}: Lấy khách sạn theo ID
- GET /user/{userId}: Lấy các khách sạn của một user
- POST /: Tạo khách sạn mới (yêu cầu đăng nhập)
- PUT /{id}: Cập nhật khách sạn (chủ sở hữu/Management)
- DELETE /{id}: Xóa khách sạn (chủ sở hữu/Admin)
- GET /{id}/rooms: Lấy danh sách phòng của khách sạn
- POST /{id}/rooms: Thêm phòng mới (chủ sở hữu/Management)
- PUT /rooms/{roomId}: Cập nhật phòng (chủ sở hữu/Management)
- DELETE /rooms/{roomId}: Xóa phòng (chủ sở hữu/Management)

## Schedules (/api/schedules)

- GET /: Lấy lịch trình của user hiện tại (đăng nhập)
- GET /{id}: Lấy chi tiết lịch trình (chủ sở hữu/Management)
- POST /: Đặt tour mới (đăng nhập)
- PUT /{id}: Cập nhật lịch trình (chủ sở hữu/Management)
- DELETE /{id}: Hủy lịch trình (chủ sở hữu/Management)
- GET /{id}/itinerary: Lấy chi tiết lịch trình (chủ sở hữu/Management)

## Bills (/api/bills)

- GET /: Lấy danh sách hóa đơn của user (đăng nhập)
- GET /{id}: Lấy chi tiết hóa đơn (chủ sở hữu/Management)
- POST /: Tạo hóa đơn mới (đăng nhập)
- PUT /{id}/status: Cập nhật trạng thái hóa đơn (Management)
- POST /{id}/payment: Thanh toán hóa đơn (chủ sở hữu)
- GET /{id}/details: Lấy chi tiết hóa đơn (chủ sở hữu/Management)

## Vouchers (/api/vouchers)

- GET /admin/all: Lấy tất cả voucher (Admin)
- GET /admin/{id}: Lấy voucher theo ID (Admin)
- GET /admin/type/{tableType}: Lấy voucher theo loại Tour/Hotel (Admin)
- GET /admin/active: Lấy voucher đang hiệu lực (Admin)
- GET /admin/expired: Lấy voucher hết hạn (Admin)
- GET /admin/statistics: Thống kê voucher (Admin)
- POST /admin: Tạo voucher mới (Admin)
- PUT /admin/{id}: Cập nhật voucher (Admin)
- DELETE /admin/{id}: Xóa voucher (Admin)
- GET /available: Danh sách voucher có thể lưu (public)
- GET /search/{code}: Tìm voucher theo mã (public)
- POST /save: Lưu voucher vào tài khoản (đăng nhập)
- GET /my-vouchers: Xem voucher đã lưu (đăng nhập)
- DELETE /my-vouchers/{userVoucherId}: Bỏ lưu voucher (đăng nhập)
- GET /user/{userId}: Xem voucher của một user (user đó/Admin)

## Messages (/api/messages)

- GET /: Lấy tin nhắn của user hiện tại (đăng nhập)
- GET /conversation/{userId}: Lấy cuộc trò chuyện với user khác (đăng nhập)
- POST /: Gửi tin nhắn mới (đăng nhập)
- PUT /{id}/read: Đánh dấu tin nhắn đã đọc (người nhận)
- DELETE /{id}: Xóa tin nhắn (người gửi/Admin)

## Feedback (/api/feedback)

- GET /{tableType}/{typeId}: Lấy feedback theo đối tượng (Tour/Hotel)
- POST /: Tạo feedback mới (đăng nhập)
- PUT /{id}: Cập nhật feedback (chủ sở hữu)
- DELETE /{id}: Xóa feedback (chủ sở hữu/Admin)
- GET /user/{userId}: Lấy feedback của một user (Management)

## Refunds (/api/refunds)

- GET /: Lấy danh sách yêu cầu hoàn tiền (Management)
- GET /{id}: Lấy chi tiết yêu cầu hoàn tiền (chủ sở hữu/Management)
- POST /: Tạo yêu cầu hoàn tiền (chủ sở hữu hóa đơn)
- PUT /{id}/status: Xử lý yêu cầu hoàn tiền (Management)
- GET /bill/{billId}: Lấy yêu cầu hoàn tiền theo hóa đơn

## Notifications (/api/notifications)

- GET /: Lấy thông báo của user hiện tại (đăng nhập)
- GET /{id}: Lấy chi tiết thông báo (người nhận/Management)
- PUT /{id}/read: Đánh dấu thông báo đã đọc (người nhận)
- DELETE /{id}: Xóa thông báo (người nhận/Admin)

## Images (/api/images)

- POST /upload: Upload ảnh (multipart/form-data) gồm: tableType, typeId, file, description (đăng nhập)
- GET /{tableType}/{typeId}: Lấy danh sách ảnh theo đối tượng
- GET /{id}: Lấy ảnh theo ID
- PUT /{id}: Cập nhật mô tả ảnh (chủ sở hữu/Management)
- DELETE /{id}: Xóa ảnh (chủ sở hữu/Management)
- DELETE /{tableType}/{typeId}/all: Xóa tất cả ảnh của đối tượng (Management)
- GET /avatar/{userId}: Lấy avatar của user
- POST /avatar/{userId}/{imageId}: Đặt ảnh làm avatar user (user đó/Admin)

## Data Types & Status

**TableType hợp lệ:**

- "Tour": Tour du lịch
- "Hotel": Khách sạn
- "HotelRoom": Phòng khách sạn
- "User": Người dùng

**User Roles:**

- "Customer": Khách hàng
- "TourGuide": Hướng dẫn viên
- "HotelManager": Quản lý khách sạn
- "Admin": Quản trị viên

**Status Types:**

- Tour/Hotel Status: "Active", "Inactive", "Pending"
- Bill Status: "Pending", "Paid", "Cancelled", "Refunded"

**Technical Stack:**

- .NET 8 + Entity Framework Core
- JWT Authentication + Redis Session
- SendGrid Email + Cloudinary Images
- SQL Server Database
