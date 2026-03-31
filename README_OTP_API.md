# 📧 Phục hồi Mật khẩu qua Email (OTP - Resend.com)

Hệ thống cung cấp luồng phục hồi mật khẩu an toàn thông qua mã xác thực (OTP) gửi trực tiếp tới Email của người dùng nhờ dịch vụ **Resend.com**.

---

## 🏗️ Luồng xử lý (Workflow)

1. **Yêu cầu mã mã OTP**: Gọi API `/send-otp` với Số điện thoại. Hệ thống sẽ tìm User, sinh mã 6 số ngẫu nhiên, lưu vào Cache (10 phút) và gửi tới Email tương ứng.
2. **Đặt lại mật khẩu**: Gọi API `/reset-password` với Mã OTP nhận được + Mật khẩu mới. Hệ thống xác thực mã và băm (Hash) mật khẩu mới để lưu vào Database.

---

## 📡 Danh sách API

### 1. Gửi mã OTP (Step 1)
**Endpoint:** `POST /api/Auth/forgot-password/send-otp`  
**Quyền truy cập:** Nặc danh (`[AllowAnonymous]`)

- **Request Body:**
  ```json
  { "phone": "0987654321" }
  ```
- **Xử lý:** 
    * Nếu gửi lại quá nhanh (< 60s), hệ thống trả về lỗi Cooldown.
    * Mã có hiệu lực trong 10 phút.

### 2. Đặt lại mật khẩu (Step 2)
**Endpoint:** `POST /api/Auth/forgot-password/reset-password`  
**Quyền truy cập:** Nặc danh (`[AllowAnonymous]`)

- **Request Body:**
  ```json
  {
    "phone": "0987654321",
    "otp": "123456",
    "newPassword": "MậtKhẩuMới123"
  }
  ```
- **Xử lý:** Kiểm tra OTP khớp -> Lưu Password mới (BCrypt) -> Xóa OTP khỏi Cache.

---

## 🛠️ Các tính năng bảo mật tích hợp

1.  **Chống Spam (Cooldown 60s):** Người dùng không thể yêu cầu mã OTP liên tục. Phải chờ 60 giây giữa mỗi lần gửi để tránh tốn tài nguyên Email.
2.  **Che mờ Email (Privacy):** Khi gửi mã thành công, hệ thống chỉ trả về email đã che mờ (VD: `admi****@gmail.com`) để tránh lộ thông tin người dùng.
3.  **Tự hủy mã (Auto-expire):** Mã OTP được lưu trong `MemoryCache` và tự động bị hủy sau 10 phút hoặc ngay sau khi xác thực thành công.
4.  **Swagger Friendly:** Đã cấu hình để 2 API này không hiện dấu khóa 🔒, giúp Frontend dễ dàng thử nghiệm.

---

## 📁 File liên quan
- `API/Service/EmailService.cs`: Tích hợp REST API của Resend.com.
- `API/Service/AuthService.cs`: Xử lý logic sinh mã, Cooldown và DB.
- `API/Controllers/AuthController.cs`: Đầu vào của 2 API phục hồi mật khẩu.
- `appsettings.json`: Chứa `ResendSettings` (ApiKey & FromEmail).
