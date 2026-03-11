# Admin API - Hệ thống quản lý người dùng và phân quyền

Tài liệu này hướng dẫn sử dụng các endpoint dành riêng cho vai trò **ADMIN** để quản lý người dùng, phân quyền (role) và kiểm soát trạng thái hoạt động của tài khoản trong hệ thống Cứu hộ lũ lụt.

## 1. Thông tin chung
- **Base URL:** `/api/Admin`
- **Quyền truy cập:** Yêu cầu Token JWT với Role là `ADMIN`.
- **Định dạng dữ liệu:** JSON.

---

## 2. Các API Endpoints

### 2.1 Lấy danh sách người dùng hoặc Tìm kiếm theo ID
Lấy toàn bộ danh sách người dùng hoặc tìm kiếm một người dùng cụ thể bằng ID.

- **Endpoint:** `GET /api/Admin/users`
- **Query Parameter:** `userId` (int, optional) - Nhập ID để tìm kiếm, để trống để lấy tất cả.
- **Response thành công (200 OK):**
```json
{
  "success": true,
  "total": 1,
  "data": [
    {
      "userId": 1,
      "username": "admin",
      "fullName": "Nguyễn Văn An",
      "phone": "0901234567",
      "email": "admin@rescue.vn",
      "role": "ADMIN",
      "isActive": true,
      "createdAt": "2026-02-24T13:24:32.518Z"
    }
  ]
}
```

### 2.2 Lấy danh sách các Role
Hiển thị các quyền (role) hợp lệ có trong hệ thống để Admin lựa chọn gán cho người dùng.

- **Endpoint:** `GET /api/Admin/roles`
- **Response thành công (200 OK):**
```json
{
  "success": true,
  "data": ["ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN"]
}
```

### 2.3 Cập nhật Role cho người dùng
Gán quyền truy cập mới cho một người dùng cụ thể. Quyền này sẽ có hiệu lực trong lần đăng nhập tiếp theo của người dùng (hoặc khi token được làm mới).

- **Endpoint:** `PUT /api/Admin/users/{id}/role`
- **Request Body:**
```json
{
  "role": "COORDINATOR"
}
```
- **Response thành công (200 OK):**
```json
{
  "success": true,
  "message": "Đã cập nhật role cho người dùng citizen1 thành COORDINATOR"
}
```

### 2.4 Kích hoạt / Vô hiệu hóa người dùng
Bật hoặc tắt khả năng đăng nhập và sử dụng hệ thống của một tài khoản.

- **Endpoint:** `PUT /api/Admin/users/{id}/status`
- **Request Body:**
```json
{
  "isActive": false
}
```
- **Response thành công (200 OK):**
```json
{
  "success": true,
  "message": "Đã vô hiệu hóa tài khoản citizen1"
}
```

---

## 3. Quy trình nghiệp vụ (Admin Workflow)

1. **Xem danh sách:** Admin truy cập trang Quản lý người dùng, hệ thống gọi `GET /api/Admin/users`.
2. **Chọn người dùng:** Admin chọn một người dùng từ danh sách để chỉnh sửa.
3. **Xem role:** Hệ thống hiển thị các role sẵn có qua `GET /api/Admin/roles`.
4. **Cập nhật:**
   - Để đổi quyền: Admin chọn role mới và gửi yêu cầu qua `PUT /api/Admin/users/{id}/role`.
   - Để khóa/mở khóa: Admin gạt nút trạng thái và gửi yêu cầu qua `PUT /api/Admin/users/{id}/status`.
5. **Kiểm tra:** Người dùng sau khi được cập nhật sẽ có quyền hạn tương ứng với Role mới khi đăng nhập vào hệ thống.

---

## 4. Lưu ý quan trọng
- **Bảo mật:** Chỉ người dùng có role `ADMIN` mới có thể gọi các API này. Mọi nỗ lực truy cập từ các role khác (Manager, Coordinator,...) sẽ nhận lỗi `403 Forbidden`.
- **Tự vô hiệu hóa:** Hệ thống ngăn chặn Admin tự vô hiệu hóa tài khoản của chính mình để tránh tình trạng hệ thống không còn người quản trị.
- **Dữ liệu:** Các thay đổi về Role và Trạng thái sẽ được cập nhật trực tiếp vào bảng `users` trong cơ sở dữ liệu.
