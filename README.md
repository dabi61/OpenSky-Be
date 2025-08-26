# BE_OPENSKY - Tài liệu API

Các API sẽ được cập nhật thêm sau (Cường - 26/08/2025)

## Authentication (/api/auth)

- POST /register: Đăng ký tài khoản mới
- POST /login: Đăng nhập, trả về JWT token
- POST /change-password: Đổi mật khẩu (yêu cầu đăng nhập)

## Users (/api/users)

- GET /: Lấy danh sách người dùng (Management)
- GET /{id}: Lấy người dùng theo ID (Management)
- PUT /{id}: Cập nhật thông tin người dùng (chủ sở hữu/Management)
- DELETE /{id}: Xóa người dùng (Admin)

## Tours (/api/tours)

- GET /: Lấy tất cả tour
- GET /{id}: Lấy tour theo ID
- GET /user/{userId}: Lấy các tour của một user
- POST /: Tạo tour mới (yêu cầu đăng nhập)
- PUT /{id}: Cập nhật tour (chủ sở hữu/Management)
- DELETE /{id}: Xóa tour (chủ sở hữu/Admin)

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

## Images (/api/images)

- POST /upload: Upload ảnh (multipart/form-data) gồm: tableType, typeId, file, description (đăng nhập)
- GET /{tableType}/{typeId}: Lấy danh sách ảnh theo đối tượng
- GET /{id}: Lấy ảnh theo ID
- PUT /{id}: Cập nhật mô tả ảnh (chủ sở hữu/Management)
- DELETE /{id}: Xóa ảnh (chủ sở hữu/Management)
- DELETE /{tableType}/{typeId}/all: Xóa tất cả ảnh của đối tượng (Management)
- GET /avatar/{userId}: Lấy avatar của user
- POST /avatar/{userId}/{imageId}: Đặt ảnh làm avatar user (user đó/Admin)

Ghi chú: tableType hợp lệ: "Tour", "Hotel", "HotelRoom", "User".
