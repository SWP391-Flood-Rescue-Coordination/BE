# Hướng dẫn sử dụng API cho Rescue Coordinator

## Tổng quan
Chức năng cho phép **Rescue Coordinator** xem và quản lý toàn bộ các yêu cầu cứu hộ từ người dân (bao gồm cả người dùng đã đăng nhập và khách vãng lai không đăng nhập).

## API Endpoints mới

### 1. Xem tất cả yêu cầu cứu hộ & Tìm kiếm chuẩn hóa
- **Endpoint**: `GET /api/RescueRequest`
- **Quyền hạn**: `Roles = "COORDINATOR, ADMIN, MANAGER"`
- **Tham số (Query Params)**:
    - `status` (string, optional): Lọc theo trạng thái (ví dụ: `PENDING`, `IN_PROGRESS`, `COMPLETED`).
    - `priorityId` (int, optional): Lọc theo mức độ ưu tiên (1-4).
    - `searchBy` (string, optional): Trường cần tìm. Whitelist: `requestId`, `phone`, `contactPhone`, `address`, `title`, `citizenName`, `contactName`.
    - `keyword` (string, optional): Từ khóa tìm kiếm.
- **Mô tả**: Trả về danh sách toàn bộ các yêu cầu. Hệ thống đã chuẩn hóa để mỗi trang FE chỉ search đúng 1 field nghiệp vụ, tránh nhiễu dữ liệu.
- **Quy tắc**: Không dùng 1 từ khóa quét nhiều cột bằng OR. Các trường ID/SĐT/Tên phải được chọn rõ qua `searchBy`.

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
3.  **Rescue Coordinator** truy cập Swagger hoặc Frontend để gọi `GET /api/RescueRequest` để xem danh sách.
4.  **Rescue Coordinator** nhấn "Try it out" trên Swagger để kiểm tra dữ liệu trả về.
5.  Dựa trên tình hình, Coordinator gọi `PUT /api/Coordinator/update-request/{id}` để cập nhật mức độ ưu tiên (ví dụ: gán Priority 4 cho trường hợp khẩn cấp).

## Lưu ý về bảo mật
- Endpoint này được bảo vệ bởi **JWT Token**.
- Chỉ những User có Claim `Role` là `COORDINATOR` hoặc `ADMIN` mới có thể truy cập thành công. Các Role khác sẽ nhận lỗi `403 Forbidden`.
