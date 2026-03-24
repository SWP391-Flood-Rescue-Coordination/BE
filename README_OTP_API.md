# 📱 Quên Mật Khẩu – Xác thực mã OTP (Mock SMS)

Tài liệu này mô tả tính năng **Quên mật khẩu** sử dụng mã xác thực OTP giả lập (Mock), phù hợp để test local và deploy trên các nền tảng serverless như Vercel.

---

## 🏗️ Kiến trúc

```
FE (người dùng)
    │
    ├─ POST /api/auth/forgot-password/send-otp       ← Bước 1: Gửi yêu cầu
    └─ POST /api/auth/forgot-password/reset-password ← Bước 2: Nhập OTP + pass mới
         │
         ▼
    AuthController
         │
         ▼
    AuthService
         │
         ├─ Kiểm tra SĐT tồn tại trong DB
         └─ MockSmsService (Xác thực mã 123456)
```

### Tại sao dùng Mock OTP cố định (123456)?
Hệ thống sử dụng mã OTP cố định để hỗ trợ việc test và demo nhanh chóng mà không cần tích hợp nhà mạng (SMS gateway) tốn phí. Đặc biệt giúp tránh lỗi mất bộ nhớ khi deploy trên **Vercel** (Stateless environment).

---

## 📡 API Reference

### 1. `POST /api/auth/forgot-password/send-otp`

**Mô tả:** Gửi yêu cầu xác thực quên mật khẩu.

**Authentication:** Không yêu cầu

#### Request Body
```json
{
  "phone": "0912345678"
}
```

#### Response – Thành công `200 OK`
```json
{
  "success": true,
  "message": "OTP đã được gửi tới số điện thoại của bạn. (Mã test: 123456)"
}
```

---

### 2. `POST /api/auth/forgot-password/reset-password`

**Mô tả:** Xác thực OTP và đặt lại mật khẩu mới.

**Authentication:** Không yêu cầu

#### Request Body
```json
{
  "phone": "0912345678",
  "otp": "123456",
  "newPassword": "matkhaumoi123"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `phone` | string | Số điện thoại đã đăng ký |
| `otp` | string | Nhập mã xác thực cố định: **123456** |
| `newPassword` | string | Mật khẩu mới (tối thiểu 5 ký tự) |

#### Response – Thành công `200 OK`
```json
{
  "success": true,
  "message": "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập lại."
}
```

---

## 🔄 Flow hoàn chỉnh

1. **Người dùng** nhập số điện thoại.
2. **Backend** kiểm tra số điện thoại có trong DB không.
3. **Backend** phản hồi thành công và log ra console thông báo sẵn sàng nhận mã.
4. **Người dùng** nhập mã OTP mặc định là **123456**.
5. **Backend** xác thực mã, hash password mới bằng BCrypt và lưu vào DB.

---

## 🛡️ Bảo mật (Environment)

Hệ thống được thiết kế theo Interface `ISmsService`. Trong tương lai, nếu muốn dùng SMS thật (Twilio, Vonage...), bạn chỉ cần tạo một class mới implement interface này mà không cần sửa bất kỳ logic nào trong `AuthService` hay `AuthController`.

---

## 📁 Files liên quan

- `API/DTOs/OtpDtos.cs`: Các model request/response.
- `API/Service/ISmsService.cs`: Interface định nghĩa các phương thức xác thực.
- `API/Service/MockSmsService.cs`: Logic xử lý mã OTP cố định (123456).
- `API/Service/AuthService.cs`: Điều phối luồng quên mật khẩu.
- `API/Controllers/AuthController.cs`: Các endpoint API công khai.
