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
        string UserStatus
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
        decimal Latitude
        decimal Longitude
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
        string RoomType
        string Address
        decimal Price
        int MaxPeople
        string Status
        string RoomStatus
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

    Booking {
        Guid BookingID PK
        Guid UserID FK
        Guid HotelID FK
        Guid TourID FK
        DateTime CheckInDate
        DateTime CheckOutDate
        string Status
        string Notes
        string PaymentMethod
        string PaymentStatus
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    Bill {
        Guid BillID PK
        Guid UserID FK
        Guid BookingID FK UK
        Guid UserVoucherID FK
        decimal Deposit
        decimal RefundPrice
        decimal TotalPrice
        string Status
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    BillDetail {
        Guid BillDetailID PK
        Guid BillID FK
        string ItemType
        Guid ItemID
        Guid RoomID
        Guid ScheduleID
        string ItemName
        int Quantity
        decimal UnitPrice
        decimal TotalPrice
        string Notes
        DateTime CreatedAt
        DateTime UpdatedAt
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
    User ||--o{ Booking : "creates"
    User ||--o{ Schedule : "books"
    User ||--o{ FeedBack : "gives"
    User ||--o{ UserVoucher : "saves"

    Hotel ||--o{ HotelRoom : "contains"
    Hotel ||--o{ Booking : "has bookings"

    Booking ||--|| Bill : "one-to-one"

    Tour ||--o{ Schedule : "scheduled for"
    Tour ||--o{ TourItinerary : "has itinerary"
    Tour ||--o{ Booking : "booked for"

    Bill ||--o{ BillDetail : "contains"
    Bill ||--o{ Refund : "may have"
    Bill ||--o{ Notification : "generates"
    Bill }o--|| UserVoucher : "uses voucher"

    Schedule ||--o{ ScheduleItinerary : "has detailed itinerary"

    TourItinerary ||--o{ ScheduleItinerary : "scheduled in"

    Voucher ||--o{ UserVoucher : "saved by users"
```

Ghi chú: PK = Primary Key, FK = Foreign Key, UK = Unique Key

### 🔄 Cập nhật Schema gần đây

#### Hotel Table Changes:

- **Coordinates** → **Latitude** & **Longitude** (decimal type với precision cao)
- **HotelStatus** thêm: `Suspend` (tạm ngưng), `Removed` (đã xóa)

#### HotelRoom Table Changes:

- Thêm field **RoomStatus** để quản lý trạng thái phòng chi tiết

#### User Table Changes:

- Thêm field **UserStatus** để quản lý trạng thái tài khoản

#### Booking System Changes:

- **BillDetail** hỗ trợ multiple rooms trong 1 booking
- **Quantity** field = (số phòng) × (số đêm)
- API endpoint `/detail` cung cấp thông tin đầy đủ với BillDetail

## 📊 Các bảng mới được thêm

### Booking Table

- **Mục đích**: Quản lý đặt phòng khách sạn và tour
- **Tính năng**: Hỗ trợ cả hotel booking và tour booking
- **Trạng thái**: Pending → Confirmed → Completed
- **Payment**: Liên kết với Bill để quản lý thanh toán

### Hotel Table (Cập nhật)

- **Coordinates** → **Latitude & Longitude**: Chuyển từ string sang decimal với precision cao (18,15)
- **HotelStatus**: Thêm `Suspend` (tạm ngưng) và `Removed` (đã xóa)
- **Tính năng**: Hỗ trợ định vị chính xác với lat/lon

### Bill & BillDetail Tables (Cập nhật)

- **Bill**: Quản lý hóa đơn với payment method và transaction ID
- **BillDetail**: Chi tiết hóa đơn với item type (Hotel/Tour)
- **Tính năng**: Hỗ trợ voucher, refund, và payment tracking
- **Multiple Rooms**: Hỗ trợ đặt nhiều phòng trong 1 booking

### HotelRoom Table (Cập nhật)

- **RoomType**: Chuyển từ int sang string (Deluxe, Standard, Suite...)
- **MaxPeople**: Số người tối đa cho phòng
- **Status**: Trạng thái phòng (Available, Occupied, Maintenance)
- **RoomStatus**: Trạng thái chi tiết của phòng (Available, Occupied, Maintenance, OutOfOrder)

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

## Hotels (/api/hotels)

### Hotel Application & Management

**Customer & Admin Functions:**

- POST /hotels/apply: Customer đăng ký mở khách sạn
- GET /hotels/pending: Admin xem tất cả đơn đăng ký khách sạn chờ duyệt
- GET /hotels/pending/{hotelId}: Admin xem chi tiết đơn đăng ký
- POST /hotels/approve/{hotelId}: Admin duyệt đơn đăng ký khách sạn
- DELETE /hotels/reject/{hotelId}: Admin từ chối đơn đăng ký khách sạn
- GET /hotels/my-hotels: Customer xem khách sạn của mình

**Hotel Owner Functions:**

- PUT /hotels/{hotelId}: Cập nhật thông tin khách sạn
- POST /hotels/{hotelId}/images: Thêm nhiều ảnh cho khách sạn
- GET /hotels/{hotelId}: Xem chi tiết khách sạn (có phân trang phòng)
- GET /hotels/search: Tìm kiếm và lọc khách sạn (Public - không cần auth)

## Hotel Rooms (/api/hotels)

### Room Management (Chủ khách sạn)

- POST /hotels/{hotelId}/rooms: Thêm phòng mới cho khách sạn (hỗ trợ upload ảnh cùng lúc)
- GET /hotels/rooms/{roomId}: Xem chi tiết phòng
- GET /hotels/{hotelId}/rooms: Danh sách phòng có phân trang
- PUT /hotels/rooms/{roomId}: Cập nhật thông tin phòng
- DELETE /hotels/rooms/{roomId}: Xóa phòng
- PUT /hotels/rooms/{roomId}/status: Cập nhật trạng thái phòng
- GET /hotels/{hotelId}/rooms/status: Xem danh sách phòng theo trạng thái

### Tạo phòng mới với ảnh

**Endpoint:** `POST /api/hotels/{hotelId}/rooms`

**Mô tả:** Tạo phòng mới cho khách sạn và upload ảnh cùng lúc trong 1 request.

**Content-Type:** `multipart/form-data`

**Form Fields:**

- `roomName` (string, required): Tên phòng
- `roomType` (string, required): Loại phòng (Deluxe, Standard, Suite...)
- `address` (string, required): Địa chỉ phòng
- `price` (decimal, required): Giá phòng/đêm
- `maxPeople` (int, required): Số người tối đa (1-20)
- `files` (file[], optional): Danh sách ảnh (JPEG, PNG, GIF, WebP, max 5MB/file)

**Request Example:**

```http
POST /api/hotels/123e4567-e89b-12d3-a456-426614174000/rooms
Content-Type: multipart/form-data

roomName: Deluxe Ocean View
roomType: Deluxe
address: Tầng 5, Phòng 501
price: 1500000
maxPeople: 4
files: [room1.jpg, room2.jpg, room3.jpg]
```

**Response Example:**

```json
{
  "roomID": "456e7890-e89b-12d3-a456-426614174001",
  "message": "Tạo phòng thành công với 3 ảnh",
  "uploadedImageUrls": [
    "https://res.cloudinary.com/example/image/upload/v1234567890/rooms/room1.jpg",
    "https://res.cloudinary.com/example/image/upload/v1234567890/rooms/room2.jpg",
    "https://res.cloudinary.com/example/image/upload/v1234567890/rooms/room3.jpg"
  ],
  "failedUploads": [],
  "successImageCount": 3,
  "failedImageCount": 0
}
```

**Lưu ý:**

- Nếu không upload ảnh, phòng vẫn được tạo thành công
- Ảnh được upload lên Cloudinary và lưu vào database tự động
- Hỗ trợ upload nhiều ảnh cùng lúc
- Nếu có lỗi upload ảnh, phòng vẫn được tạo và thông báo lỗi chi tiết

## Hotel Reviews (/api/hotels)

### Review Management (Đánh giá khách sạn)

**Điều kiện:** Chỉ có thể đánh giá sau khi đã đặt phòng và thanh toán thành công

- POST /hotels/{hotelId}/reviews: Tạo đánh giá khách sạn (1-5 sao)
- GET /hotels/{hotelId}/reviews/eligibility: Kiểm tra điều kiện đánh giá
- PUT /hotels/{hotelId}/reviews/{reviewId}: Cập nhật đánh giá
- DELETE /hotels/{hotelId}/reviews/{reviewId}: Xóa đánh giá
- GET /hotels/{hotelId}/reviews/{reviewId}: Xem đánh giá theo ID
- GET /hotels/{hotelId}/reviews: Danh sách đánh giá (có phân trang)
- GET /hotels/{hotelId}/reviews/stats: Thống kê đánh giá
- GET /hotels/my-reviews: Đánh giá của user hiện tại

## Bill Management (/api/bills)

### Bill & Payment Endpoints

- POST /bills/qr/create: Tạo QR code thanh toán cho hóa đơn
- GET /bills/{billId}: Lấy thông tin hóa đơn theo ID

## Booking Management (/api/bookings)

### Booking Endpoints

**Đặt phòng:**

- POST /bookings: Đặt phòng khách sạn (hỗ trợ cả 1 phòng và nhiều phòng)
- GET /bookings/my-bookings: Xem booking của tôi (có phân trang)
- GET /bookings/{bookingId}/detail: Xem chi tiết booking với BillDetail
- PUT /bookings/{bookingId}/cancel: Hủy booking

**Quản lý:**

- GET /bookings/check-availability: Kiểm tra phòng có sẵn
- GET /bookings/stats: Thống kê booking (Admin/Hotel)
- PUT /bookings/{bookingId}/payment-status: Cập nhật trạng thái thanh toán
- PUT /bookings/{bookingId}/status: Cập nhật trạng thái booking
- POST /bookings/{bookingId}/check-in: Check-in booking
- POST /bookings/{bookingId}/check-out: Check-out booking

**Thanh toán:**

- POST /bookings/{billId}/qr-payment: Tạo QR code thanh toán
- GET /bookings/{billId}/payment-status: Kiểm tra trạng thái thanh toán QR

### Room Booking (1 hoặc nhiều phòng)

**Request Example - 1 phòng:**

```json
POST /api/bookings
{
  "rooms": [
    {
      "roomID": "guid-1",
      "quantity": 1
    }
  ],
  "checkInDate": "2024-01-15",
  "checkOutDate": "2024-01-17"
}
```

**Request Example - Nhiều phòng:**

```json
POST /api/bookings
{
  "rooms": [
    {
      "roomID": "guid-1",
      "quantity": 2
    },
    {
      "roomID": "guid-2",
      "quantity": 1
    }
  ],
  "checkInDate": "2024-01-15",
  "checkOutDate": "2024-01-17"
}
```

**Response Example:**

```json
{
  "message": "Đặt 3 phòng thành công",
  "bookingId": "guid",
  "totalRooms": 3,
  "roomCount": 2
}
```

### Chi tiết Booking với BillDetail

**Request:**

```http
GET /api/bookings/{bookingId}/detail
```

**Response Example:**

```json
{
  "bookingID": "guid",
  "userID": "guid",
  "userName": "Nguyễn Văn A",
  "userEmail": "user@example.com",
  "hotelID": "guid",
  "hotelName": "Hotel ABC",
  "hotelAddress": "123 Đường ABC, Quận 1, TP.HCM",
  "checkInDate": "2024-01-15T00:00:00Z",
  "checkOutDate": "2024-01-17T00:00:00Z",
  "numberOfNights": 2,
  "status": "Confirmed",
  "notes": null,
  "paymentMethod": null,
  "paymentStatus": null,
  "billID": "guid",
  "totalPrice": 1500000,
  "deposit": 0,
  "billStatus": "Pending",
  "roomDetails": [
    {
      "roomID": "guid-1",
      "roomName": "Deluxe Room",
      "roomType": "Deluxe",
      "quantity": 2,
      "unitPrice": 500000,
      "totalPrice": 2000000,
      "notes": "Booking 2 phòng từ 15/01/2024 đến 17/01/2024"
    },
    {
      "roomID": "guid-2",
      "roomName": "Suite Room",
      "roomType": "Suite",
      "quantity": 1,
      "unitPrice": 800000,
      "totalPrice": 1600000,
      "notes": "Booking 1 phòng từ 15/01/2024 đến 17/01/2024"
    }
  ],
  "createdAt": "2024-01-10T10:30:00Z",
  "updatedAt": "2024-01-10T10:30:00Z"
}
```

**Điều kiện:**

- Tất cả phòng phải cùng 1 khách sạn
- Tất cả phòng phải available
- Kiểm tra xung đột thời gian cho từng phòng
- Tạo 1 Booking với nhiều BillDetail

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
