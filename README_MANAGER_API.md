# Hướng dẫn sử dụng API dành cho Manager (Quản lý Phương tiện)

Tài liệu này hướng dẫn chi tiết cách sử dụng các API dành riêng cho vai trò **Manager**.

## 1. Quản lý Phương tiện (Vehicle Management)

Dành cho màn hình quản lý chi tiết phương tiện, cho phép Manager xem danh sách và chỉnh sửa trạng thái. Các API này được viết trực tiếp trong `ManagerController` để tối giản hóa code.

### 1.1. Lấy danh sách phương tiện
- **Endpoint:** `GET /api/manager/vehicles`
- **Quyền truy cập:** `MANAGER`
- **Tham số (Query strings):**
  - `status` (String, optional): Lọc theo trạng thái (ví dụ: `Available`, `InUse`, `Maintenance`).
- **Mô tả:** Trả về toàn bộ danh sách phương tiện kèm theo thông tin loại phương tiện.

### 1.2. Cập nhật trạng thái phương tiện
- **Endpoint:** `PATCH /api/manager/vehicles/{id}/status`
- **Quyền truy cập:** `MANAGER`
- **Body (JSON):**
```json
{
  "status": "Ready"
}
```
- **Mô tả:** Cho phép Manager thay đổi nhanh trạng thái của một phương tiện.

## 2. Quản lý Kho & Điều phối Cứu trợ (Relief & Inventory)

### 2.1. Tạo phiếu xuất kho điều phối vật tư
- **Endpoint:** `POST /api/manager/relief-export`
- **Quyền truy cập:** `MANAGER`
- **Mô tả:** Tạo lệnh xuất kho dựa trên tồn kho và điều phối phương tiện.
- **Tài liệu chi tiết:** [README_RELIEF_EXPORT_API.md](file:///e:/SWP391/BE-Tuan/README_RELIEF_EXPORT_API.md)

### 2.2. Xem danh sách vật tư và lịch sử kho
- **Endpoint:** `GET /api/manager/supplies` và `GET /api/manager/supplies/history`
- **Tài liệu chi tiết:** [README_RELIEF_EXPORT_API.md](file:///e:/SWP391/BE-Tuan/README_RELIEF_EXPORT_API.md)

## 3. Xử lý lỗi
- **401 Unauthorized:** Token không hợp lệ hoặc hết hạn.
- **403 Forbidden:** Người dùng không có vai trò `MANAGER`.
- **404 Not Found:** Không tìm thấy phương tiện với ID tương ứng.
