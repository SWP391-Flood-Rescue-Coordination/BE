# Hướng dẫn sử dụng API cho Rescue Coordinator

## Tổng quan
Chức năng cho phép **Rescue Coordinator** xem và quản lý toàn bộ các yêu cầu cứu hộ từ người dân (bao gồm cả người dùng đã đăng nhập và khách vãng lai không đăng nhập).

## API Endpoints mới

### 1. Xem tất cả yêu cầu cứu hộ
- **Endpoint**: `GET /api/Coordinator/all-requests`
- **Quyền hạn**: `Roles = "COORDINATOR, ADMIN"`
- **Tham số (Query Params)**:
    - `status` (string, optional): Lọc theo trạng thái (ví dụ: `PENDING`, `IN_PROGRESS`, `COMPLETED`).
    - `priorityId` (int, optional): Lọc theo mức độ ưu tiên (1-4).
- **Mô tả**: Trả về danh sách toàn bộ các yêu cầu. Đối với khách vãng lai, thông tin tên và số điện thoại sẽ được lấy từ các trường liên hệ dự phòng.

### 2. Cập nhật yêu cầu cứu hộ
- **Endpoint**: `PUT /api/Coordinator/update-request/{id}`
- **Quyền hạn**: `Roles = "COORDINATOR, ADMIN"`
- **Body (JSON)**:
    ```json
    {
      "status": "IN_PROGRESS",
      "priorityLevelId": 3
    }
    ```
- **Mô tả**: Cho phép Coordinator cập nhật trạng thái xử lý và gán mức độ ưu tiên cho một yêu cầu.

## Luồng hoạt động tiêu chuẩn
1.  **Citizen** hoặc **Khách vãng lai** tạo request qua `POST /api/RescueRequest`.
2.  **Rescue Coordinator** đăng nhập vào hệ thống.
3.  **Rescue Coordinator** truy cập Swagger hoặc Frontend để gọi `GET /api/Coordinator/all-requests` để xem danh sách.
4.  **Rescue Coordinator** nhấn "Try it out" trên Swagger để kiểm tra dữ liệu trả về.
5.  Dựa trên tình hình, Coordinator gọi `PUT /api/Coordinator/update-request/{id}` để cập nhật mức độ ưu tiên (ví dụ: gán Priority 4 cho trường hợp khẩn cấp).

## Lưu ý về bảo mật
- Endpoint này được bảo vệ bởi **JWT Token**.
- Chỉ những User có Claim `Role` là `COORDINATOR` hoặc `ADMIN` mới có thể truy cập thành công. Các Role khác sẽ nhận lỗi `403 Forbidden`.
