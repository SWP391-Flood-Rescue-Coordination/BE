# Thay đổi Backend cho module Phương tiện

Tài liệu này mô tả các thay đổi backend mới nhất cho luồng quản lý phương tiện.

## Phạm vi

Các file đã cập nhật:

- `API/Controllers/VehicleController.cs`
- `API/DTOs/VehicleDto.cs`

Không có thay đổi schema cơ sở dữ liệu trong đợt này.

## Lý do thay đổi

Luồng cũ yêu cầu người dùng tự nhập `VehicleCode` khi tạo phương tiện. Cách này chưa phù hợp nghiệp vụ vì:

- `vehicle_id` đã được database tự sinh.
- `vehicle_code` là mã nội bộ của hệ thống.
- Người dùng không cần biết hoặc tự duy trì quy ước đánh số.

Hiện tại backend tự sinh `VehicleCode` và vẫn giữ nguyên cấu trúc dữ liệu hiện có.

## Hành vi API hiện tại

### `GET /api/Vehicle`

Mục đích:

- Trả danh sách phương tiện cho màn hình Manager, Admin, Coordinator.

Hành vi:

- Nhận tham số lọc `status` (không bắt buộc).
- Chỉ chấp nhận `AVAILABLE`, `INUSE`, `MAINTENANCE`.
- Trả kèm `latitude`, `longitude` để frontend dùng luôn cho màn hình bản đồ.

### `GET /api/Vehicle/{id}`

Mục đích:

- Trả thông tin chi tiết 1 phương tiện cho màn hình chỉnh sửa.

Hành vi:

- Trả đủ các field phục vụ form quản lý như `currentLocation`, `latitude`, `longitude`, `lastMaintenance`.

### `POST /api/Vehicle`

Mục đích:

- Tạo mới phương tiện.

Quy tắc nghiệp vụ:

- `VehicleCode` được tự sinh trong backend.
- Từ chối biển số (`LicensePlate`) bị trùng.
- `VehicleTypeId` phải tồn tại.
- Khi tạo mới chỉ cho phép set thủ công `AVAILABLE` hoặc `MAINTENANCE`.
- Các trường `CurrentLocation`, `VehicleName`, `LicensePlate` được trim trước khi lưu.

Quy tắc sinh mã phương tiện:

1. Tải thông tin loại phương tiện được chọn.
2. Đọc các mã phương tiện hiện có cùng loại.
3. Nếu đã có mã cùng loại, dùng lại prefix hiện hữu.
4. Nếu chưa có, lấy prefix từ `VehicleType.TypeCode`.
5. Chuẩn hóa một số type code dài:
   - `HELICOPTER -> HELI`
   - `AMPHIBIOUS -> AMPH`
6. Lấy hậu tố số lớn nhất và tạo mã kế tiếp theo dạng `PREFIX-001`.

Ví dụ:

- `BOAT-003 -> BOAT-004`
- `HELI-001 -> HELI-002`
- `AMPH-002 -> AMPH-003`

### `PUT /api/Vehicle/{id}`

Mục đích:

- Cập nhật thông tin nghiệp vụ của phương tiện.

Field được phép sửa:

- `VehicleName`
- `VehicleTypeId`
- `LicensePlate`
- `Capacity`
- `Status`
- `CurrentLocation`
- `Latitude`
- `Longitude`
- `LastMaintenance`

Quy tắc nghiệp vụ:

- Không cho sửa `VehicleCode`.
- Từ chối biển số trùng.
- Chỉ cho đổi trạng thái thủ công sang `AVAILABLE` hoặc `MAINTENANCE`.
- Nếu phương tiện đang `INUSE`, không cho đổi thủ công sang trạng thái khác.
- `UpdatedAt` luôn được cập nhật khi sửa thành công.

### `DELETE /api/Vehicle/{id}`

Mục đích:

- Xóa phương tiện.

Quy tắc nghiệp vụ:

- Không cho xóa phương tiện đang `INUSE`.
- Trước khi xóa phải xử lý các dòng liên quan trong `RescueOperationVehicles` để tránh lỗi khóa ngoại.

## Thay đổi DTO

### `CreateVehicleDto`

Đã bỏ:

- `VehicleCode`

Lý do:

- `VehicleCode` đã được backend tự sinh.

### `UpdateVehicleDto`

Giữ:

- Chỉ các field nghiệp vụ có thể chỉnh sửa.

Không bao gồm:

- `VehicleCode`

Lý do:

- `VehicleCode` là mã định danh nội bộ, cần ổn định sau khi tạo.

## Tóm tắt validation

- `GET /api/Vehicle?status=...` sai status trả `400`.
- Tạo/sửa bị trùng biển số trả `400`.
- `VehicleTypeId` không tồn tại trả `400`.
- Set trạng thái ngoài `AVAILABLE`/`MAINTENANCE` trả `400`.
- Xóa phương tiện đang `INUSE` trả `400`.

## Tác động tới Frontend

Frontend không cần yêu cầu người dùng nhập `VehicleCode` khi tạo/sửa. Chỉ gửi các field nghiệp vụ; backend sẽ tự sinh và lưu mã nội bộ.
