# Hướng dẫn sử dụng API Manager - Xuất kho Cứu trợ

Tài liệu này hướng dẫn chi tiết về API tạo phiếu xuất kho cứu trợ, phục vụ việc điều phối vật tư từ kho đến các đơn vị nhận (Tỉnh/Xóm) kèm theo phương tiện vận chuyển.

## 1. API Tạo phiếu xuất kho (Relief Export)

- **Endpoint:** `POST /api/manager/relief-export`
- **Quyền truy cập:** `MANAGER`
- **Mô tả:** Tạo một phiếu xuất kho mới, kiểm tra quyền hạn, tồn kho, phương tiện và sức chứa.

### 1.1. Cấu trúc yêu cầu (Request Body)

```json
{
  "warehouseId": 1,
  "destinationRegionId": 2, // ID của Tỉnh/Xóm nhận
  "notes": "Xuất cứu trợ khẩn cấp đợt lũ quét",
  "items": [
    {
      "itemId": 1,
      "quantity": 500
    },
    {
      "itemId": 5,
      "quantity": 200
    }
  ],
  "vehicleIds": [10, 15] // Danh sách các xe vận chuyển
}
```

### 1.2. Các quy định kiểm tra (Business Rules)

1.  **Phạm vi quản lý (Scope):** Hệ thống kiểm tra bảng `manager_scopes`. Manager chỉ được phép xuất kho nếu `destinationRegionId` nằm trong danh sách vùng mà họ được phân công quản lý.
2.  **Trạng thái phương tiện:** Tất cả xe trong `vehicleIds` phải có trạng thái `AVAILABLE`. Nếu có xe đang bận (`InUse`) hoặc bảo trì, yêu cầu sẽ bị từ chối.
3.  **Kiểm tra tồn kho:** Kiểm tra số lượng của từng `itemId` trong `warehouseId`. Nếu số lượng yêu cầu lớn hơn tồn thực tế trong bảng `inventories`, hệ thống sẽ trả về lỗi chi tiết tên vật tư thiếu.
4.  **Kiểm tra sức chứa (Capacity):** Hệ thống tính tổng số lượng vật tư (`sum(quantity)`) và so sánh với tổng sức chứa của các xe đã chọn (`sum(vehicle.capacity)`). Nếu hàng quá tải, yêu cầu sẽ không được thực hiện.

### 1.3. Các hành động khi Submit thành công

- **Hệ thống tạo:**
    - 01 bản ghi trong `relief_export_orders` (Phiếu xuất).
    - Các bản ghi trong `relief_export_items` (Chi tiết vật tư).
    - Các bản ghi trong `relief_export_vehicles` (Liên kết xe).
    - 01 bản ghi trong `stock_history` để đồng bộ với nhật ký kho cũ của hệ thống.
- **Hệ thống cập nhật:**
    - Trừ số lượng tồn kho trong bảng `inventories`.
    - Cập nhật trạng thái các xe liên quan sang `InUse`.

## 2. Dữ liệu mẫu (Gợi ý test)

Nếu bạn chưa có dữ liệu vùng hoặc quyền, bạn cần đảm bảo các bảng `regions`, `manager_scopes`, `warehouses`, `inventories` đã có dữ liệu tương ứng gắn với `UserId` của Manager đang login.

---
*Lưu ý: API này sử dụng JWT Token để xác định Manager ID, hãy đảm bảo đính kèm Header Authorization.*
