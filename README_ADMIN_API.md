# UserInfo API (Vai trò Admin) - Hệ thống quản lý người dùng và phân quyền

Tài liệu này hướng dẫn sử dụng các endpoint dành riêng cho vai trò **ADMIN** để quản lý người dùng thông qua **UserInfoController**. Toàn bộ các API này giờ đây được tập trung tại một đầu mối duy nhất.

## 1. Thông tin chung
- **Base URL:** `/api/UserInfo`
- **Quyền truy cập:** Yêu cầu Token JWT với Role là `ADMIN`.
- **Định dạng dữ liệu:** JSON.

---

## 2. Các API Endpoints

### 2.1 Lấy danh sách người dùng & Tìm kiếm chuẩn hóa
Lấy toàn bộ danh sách người dùng hoặc tìm kiếm tập trung theo một trường cụ thể.

- **Endpoint:** `GET /api/UserInfo`
- **Tham số tìm kiếm (Query Params):**
    - `searchBy` (string, optional): Trường cần tìm kiếm. Bắt buộc nằm trong whitelist: `userId`, `username`, `fullName`, `email`, `phone`.
    - `keyword` (string, optional): Từ khóa tìm kiếm (sẽ được tự động trim khoảng trắng).
- **Quy tắc:** Nếu truyền `searchBy` không hợp lệ, hệ thống trả về `400 BadRequest`. Không hỗ trợ tìm kiếm đa cột cùng lúc để tránh nhiễu kết quả.

- **Ví dụ tìm theo ID:** `GET /api/UserInfo?searchBy=userId&keyword=1`
- **Ví dụ tìm theo tên:** `GET /api/UserInfo?searchBy=fullName&keyword=Nguyen`

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
      "email": "admin@rescue.vn",
      "phone": "0987654321",
      "role": "ADMIN",
      "isActive": true,
      "createdAt": "2024-03-12T00:00:00Z"
    }
  ]
}
```

### 2.2 Lấy danh sách các Role hợp lệ
Hiển thị các quyền (role) hợp lệ có trong hệ thống để Admin lựa chọn gán cho người dùng.

- **Endpoint:** `GET /api/UserInfo/roles`
- **Response thành công (200 OK):**
```json
{
  "success": true,
  "data": ["ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN"]
}
```

### 2.3 Cập nhật Role cho người dùng
Gán quyền truy cập mới cho một người dùng cụ thể. Theo chính sách bảo mật, Admin **không thể** tự cấp quyền `ADMIN` hoặc `MANAGER` cho bất kỳ ai (các quyền này phải do Owner/System xử lý).

- **Endpoint:** `PUT /api/UserInfo/{id}/role`
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

### 2.4 Kích hoạt / Vô hiệu hóa tài khoản
Bật hoặc tắt khả năng đăng nhập và sử dụng hệ thống của một tài khoản.

- **Endpoint:** `PUT /api/UserInfo/{id}/status`
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

## 3. Lưu ý quan trọng
- **Bảo mật:** Chỉ người dùng có role `ADMIN` mới có thể gọi các API quản lý này.
- **Tự chặn:** Hệ thống ngăn chặn Admin tự vô hiệu hóa tài khoản của chính mình để tránh mất quyền truy cập hệ thống.
- **DTO:** Tất cả các API này sử dụng chung DTO **UserInfo** cho đầu ra và các Request DTO đã được gộp trong cùng một file để dễ quản lý.
