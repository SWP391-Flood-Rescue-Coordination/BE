# Luồng Người dân báo an toàn

Ngày cập nhật: 2026-04-09

## Mục đích

Tài liệu mô tả luồng "đội hoàn tất nhiệm vụ" và "người được cứu xác nhận an toàn" giữa Backend và Frontend.

## Tóm tắt nghiệp vụ

1. Đội cứu hộ hoàn thành nhiệm vụ thực địa (operation).
2. Request chưa đóng ngay ở bước đội hoàn thành.
3. Người dân/Khách là tác nhân cuối xác nhận an toàn.
4. Request chỉ chuyển `Completed` sau bước xác nhận an toàn.

## Luồng xử lý

### Bước 1: Xác minh và phân công

- Request được xác minh và chuyển sang `Assigned`.
- Có operation liên kết với request.

### Bước 2: Chuyển operation sang Waiting (khi cần)

API:

- `PUT /api/rescue-team/operations/{operationId}/waiting`

Điều kiện:

- Member hoặc Leader của đúng team.
- Operation đang ở `Assigned`.

Kết quả:

- Operation chuyển sang `Waiting`.

### Bước 3: Đội trưởng hoàn tất nhiệm vụ

API:

- `PUT /api/rescue-team/operations/{operationId}/status`

Payload:

```json
{
  "newStatus": "COMPLETED"
}
```

Điều kiện:

- Người gọi là `Leader` thuộc đúng team.
- Operation hiện tại là `Waiting`.

Kết quả:

- `rescue_operations.status -> Completed`
- Gán `StartedAt` nếu chưa có.
- Gán `CompletedAt`.
- Giữ nguyên `rescue_requests.status`.
- Giữ nguyên `RequestId` của member.
- Trả phương tiện của operation về `Available`.

### Bước 4: Backend trả cờ cho UI

Backend trả `CanReportSafe = true` khi:

1. Request vẫn ở `Assigned`.
2. Có ít nhất một operation liên kết đã `Completed`.

### Bước 5: Người dân báo an toàn

API:

- `PUT /api/RescueRequest/{id}/confirm-rescued`

Điều kiện:

- User là chủ sở hữu request.
- Request chưa ở trạng thái đóng/hủy.
- Có operation liên kết đã `Completed`.

Kết quả:

- `rescue_requests.status -> Completed`
- Ghi lịch sử trạng thái `Completed`.
- Giải phóng member (`RequestId = null`).
- Giải phóng phương tiện (`AVAILABLE`).

### Bước 6: Guest báo an toàn

API:

- `PUT /api/RescueRequest/guest/{id}/confirm-rescued`

Payload:

```json
{
  "phone": "0912345678"
}
```

Điều kiện:

- Có `phone` và khớp `request.Phone` hoặc `request.ContactPhone`.
- Request chưa ở trạng thái đóng/hủy.
- Có operation liên kết đã `Completed`.

Kết quả:

- `rescue_requests.status -> Completed`
- Ghi lịch sử trạng thái `Completed` với `updated_by = 0`.
- Giải phóng member (`RequestId = null`).
- Giải phóng phương tiện (`AVAILABLE`).

## Ghi chú

- UI hiển thị nút "Báo an toàn" dựa trên `CanReportSafe`.
- `CanReportSafe` là cờ tính động, không phải cột vật lý trong DB.
