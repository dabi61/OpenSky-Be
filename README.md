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
        string Province
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
- POST /login: Đăng nhập, trả về access token + refresh token
- POST /change-password: Đổi mật khẩu (yêu cầu đăng nhập)
- POST /refresh: Làm mới access token bằng refresh token
- POST /logout: Đăng xuất, vô hiệu hóa refresh token
- POST /forgot-password: Gửi mã reset mật khẩu qua email
- POST /reset-password: Reset mật khẩu bằng token
- GET /validate-reset-token/{token}: Kiểm tra tính hợp lệ của reset token
- POST /test-email: Test email service (development)

## Google OAuth (/api/auth/google)

- POST /login: Đăng nhập/đăng ký bằng Google OAuth
- POST /test: Test Google OAuth (development)
- GET /config: Lấy cấu hình Google OAuth

## Users (/api/users)

### User Management

- POST /create-supervisor: Admin tạo tài khoản Supervisor
- POST /create-tourguide: Supervisor tạo tài khoản TourGuide
- GET /: Lấy danh sách người dùng (Admin/Supervisor)
- GET /profile: Xem thông tin cá nhân (đăng nhập)
- PUT /profile: Cập nhật thông tin cá nhân (đăng nhập)
- POST /profile/avatar: Upload ảnh đại diện (đăng nhập)

### Hotel Application System

- POST /apply-hotel: Customer đăng ký mở khách sạn
- GET /pending-hotels: Admin xem tất cả đơn đăng ký khách sạn chờ duyệt
- GET /pending-hotels/{hotelId}: Admin xem chi tiết đơn đăng ký
- POST /approve-hotel/{hotelId}: Admin duyệt đơn đăng ký khách sạn
- DELETE /reject-hotel/{hotelId}: Admin từ chối đơn đăng ký khách sạn
- GET /my-hotels: Customer xem khách sạn của mình

## Hotels (/api/hotels)

### Hotel Management (Chủ khách sạn)

- PUT /hotels/{hotelId}: Cập nhật thông tin khách sạn
- POST /hotels/{hotelId}/images: Thêm nhiều ảnh cho khách sạn
- POST /hotels/{hotelId}/rooms: Thêm phòng mới cho khách sạn
- POST /hotels/rooms/{roomId}/images: Thêm nhiều ảnh cho phòng
- GET /hotels/{hotelId}: Xem chi tiết khách sạn (có phân trang phòng)
- GET /hotels/rooms/{roomId}: Xem chi tiết phòng
- GET /hotels/{hotelId}/rooms: Danh sách phòng có phân trang
- PUT /hotels/rooms/{roomId}: Cập nhật thông tin phòng
- DELETE /hotels/rooms/{roomId}: Xóa phòng

## API Chưa Implement

**Các API sau chưa được phát triển:**

- Tours (/api/tours)
- Schedules (/api/schedules)
- Bills (/api/bills)
- Vouchers (/api/vouchers)
- Messages (/api/messages)
- Feedback (/api/feedback)
- Refunds (/api/refunds)
- Notifications (/api/notifications)

## User Roles & Permissions

**Customer**: Đăng ký khách sạn, xem profile
**TourGuide**: Được tạo bởi Supervisor
**Supervisor**: Tạo TourGuide, xem danh sách user
**Admin**: Tạo Supervisor, duyệt khách sạn, toàn quyền

## Technical Features

**Authentication:**

- JWT Access Token (1 giờ)
- Refresh Token (30 ngày)
- Session management với Redis
- Password reset qua email

**File Upload:**

- Avatar upload với Cloudinary
- Hỗ trợ multipart/form-data và raw binary
- Giới hạn 5MB

**Email Service:**

- SendGrid integration
- Password reset emails
- Test email endpoint

**Database:**

- Entity Framework Core
- Auto-migration on startup
- 16 tables với GUID primary keys
