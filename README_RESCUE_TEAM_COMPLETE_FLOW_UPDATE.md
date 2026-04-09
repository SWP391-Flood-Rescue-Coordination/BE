## Cập nhật luồng Hoàn tất nhiệm vụ của Đội cứu hộ

Ngày cập nhật: 2026-04-09

### Phạm vi áp dụng

Tài liệu này mô tả luồng mới khi Đội cứu hộ cập nhật trạng thái nhiệm vụ trên API.

File chính liên quan:

- `API/Controllers/RescueTeamController.cs`

Không có thay đổi schema cơ sở dữ liệu.

### Luồng nghiệp vụ chuẩn

1. Điều phối viên xác minh yêu cầu cứu hộ (`Verified`).
2. Điều phối viên/Đội trưởng phân công nhiệm vụ (`Assigned`).
3. Thành viên hoặc Đội trưởng có thể chuyển operation từ `Assigned` sang `Waiting`:
   - `PUT /api/rescue-team/operations/{operationId}/waiting`
4. Chỉ Đội trưởng được chốt nhiệm vụ:
   - `PUT /api/rescue-team/operations/{operationId}/status`
   - Chỉ cho phép `Waiting -> Completed`.
5. Người dân/Khách xác nhận an toàn:
   - `PUT /api/RescueRequest/{id}/confirm-rescued`
   - `PUT /api/RescueRequest/guest/{id}/confirm-rescued`
   - Khi đó request mới chuyển sang `Completed`.

### Quy tắc API Hoàn tất nhiệm vụ

Endpoint:

- `PUT /api/rescue-team/operations/{operationId}/status`

Ràng buộc:

- Chỉ chấp nhận `newStatus = COMPLETED`.
- Chỉ chấp nhận khi operation hiện tại là `Waiting`.
- Chỉ Đội trưởng (`Leader`) thuộc đúng team được thao tác.
- Không reset `RequestId` của thành viên ở bước này.
- Giải phóng toàn bộ phương tiện của operation về `Available`.
- `RescueRequest.Status` giữ nguyên cho đến khi Citizen/Guest xác nhận an toàn.

### Quy tắc API chuyển sang Waiting

Endpoint:

- `PUT /api/rescue-team/operations/{operationId}/waiting`

Ràng buộc:

- Role: `RESCUE_TEAM`.
- Người gọi phải là thành viên đang hoạt động của team xử lý operation.
- Chỉ cho phép chuyển từ `Assigned` sang `Waiting`.

### Ghi chú

- Bước đóng request (`Completed`) thuộc về Citizen/Guest, không tự động đóng ngay tại bước Leader complete.
- Tài liệu này ưu tiên thay đổi tối thiểu để giảm rủi ro ảnh hưởng các luồng khác.
## Cập nhật luồng Hoàn tất nhiệm vụ của Đội cứu hộ

Ngày cập nhật: 2026-04-09

### Phạm vi áp dụng

Tài liệu này mô tả luồng mới khi Đội cứu hộ cập nhật trạng thái nhiệm vụ trên API.

File chính liên quan:

- `API/Controllers/RescueTeamController.cs`

Không có thay đổi schema cơ sở dữ liệu.

### Mục tiêu

- Chuẩn hóa trạng thái nhiệm vụ theo đúng quy tắc nghiệp vụ.
- Phân tách rõ bước "Đội cứu hộ hoàn tất nhiệm vụ" và bước "Người dân/Khách báo an toàn".
- Tránh phát sinh lỗi do cập nhật trạng thái không đúng thời điểm.

### Luồng nghiệp vụ chuẩn

1. Điều phối viên xác minh yêu cầu cứu hộ (`Verified`).
2. Điều phối viên/Đội trưởng phân công nhiệm vụ (`Assigned`).
3. Thành viên hoặc Đội trưởng có thể chuyển operation từ `Assigned` sang `Waiting`:
   - `PUT /api/rescue-team/operations/{operationId}/waiting`
4. Chỉ Đội trưởng được chốt nhiệm vụ:
   - `PUT /api/rescue-team/operations/{operationId}/status`
   - Chỉ cho phép `Waiting -> Completed`.
5. Người dân/Khách xác nhận an toàn:
   - `PUT /api/RescueRequest/{id}/confirm-rescued`
   - `PUT /api/RescueRequest/guest/{id}/confirm-rescued`
   - Khi đó request mới chuyển sang `Completed`.

### Quy tắc API Hoàn tất nhiệm vụ

Endpoint:

- `PUT /api/rescue-team/operations/{operationId}/status`

Ràng buộc:

- Chỉ chấp nhận `newStatus = COMPLETED`.
- Chỉ chấp nhận khi operation hiện tại là `Waiting`.
- Chỉ Đội trưởng (`Leader`) thuộc đúng team được thao tác.
- Không reset `RequestId` của thành viên ở bước này.
- Giải phóng toàn bộ phương tiện của operation về `Available`.
- `RescueRequest.Status` giữ nguyên cho đến khi Citizen/Guest xác nhận an toàn.

### Quy tắc API chuyển sang Waiting

Endpoint:

- `PUT /api/rescue-team/operations/{operationId}/waiting`

Ràng buộc:

- Role: `RESCUE_TEAM`.
- Người gọi phải là thành viên đang hoạt động của team xử lý operation.
- Chỉ cho phép chuyển từ `Assigned` sang `Waiting`.

### Xử lý lỗi

API trả lỗi nghiệp vụ rõ ràng cho các trường hợp:

- Dữ liệu đầu vào không hợp lệ.
- Không tìm thấy operation/request.
- Người gọi không đúng quyền hoặc không thuộc team.
- Trạng thái hiện tại không thỏa điều kiện chuyển.
- Lỗi lưu dữ liệu ở tầng database.

### Ghi chú

- Bước đóng request (`Completed`) thuộc về Citizen/Guest, không tự động đóng ngay tại bước Leader complete.
- Tài liệu này ưu tiên thay đổi tối thiểu để giảm rủi ro ảnh hưởng các luồng khác.
## Cập nhật luồng Hoàn tất nhiệm vụ của Đội cứu hộ

Ngày cập nhật: 2026-04-09

### Phạm vi áp dụng

Tài liệu này mô tả luồng mới khi Đội cứu hộ xử lý trạng thái nhiệm vụ, theo đúng nghiệp vụ hiện tại.

File chính liên quan:

- `API/Controllers/RescueTeamController.cs`

Không có thay đổi schema database.

### Mục tiêu

- Chuẩn hóa luồng cập nhật trạng thái `RescueOperation`.
- Tách rõ bước Đội cứu hộ hoàn thành nhiệm vụ và bước Người dân/Khách báo an toàn.
- Tránh các lỗi ghi lịch sử trạng thái không cần thiết.

### Luồng nghiệp vụ chuẩn

1. Điều phối viên xác minh yêu cầu cứu hộ (`Verified`).
2. Điều phối viên/Đội trưởng phân công nhiệm vụ (`Assigned`).
3. Thành viên hoặc Đội trưởng có thể chuyển operation từ `Assigned` sang `Waiting`:
   - `PUT /api/rescue-team/operations/{operationId}/waiting`
4. Chỉ Đội trưởng được phép chốt nhiệm vụ:
   - `PUT /api/rescue-team/operations/{operationId}/status`
   - Chỉ cho phép `Waiting -> Completed`.
5. Người dân/Khách báo an toàn:
   - `PUT /api/RescueRequest/{id}/confirm-rescued`
   - `PUT /api/RescueRequest/guest/{id}/confirm-rescued`
   - Khi đó request mới chuyển sang `Completed`.

### Quy tắc cho API Hoàn tất nhiệm vụ

Endpoint:

- `PUT /api/rescue-team/operations/{operationId}/status`

Ràng buộc:

- Chỉ chấp nhận `newStatus = COMPLETED`.
- Chỉ chấp nhận khi operation hiện tại là `Waiting`.
- Chỉ Đội trưởng (`Leader`) của đúng team mới có quyền thao tác.
- Không reset `RequestId` của thành viên ở bước này.
- Giải phóng toàn bộ phương tiện của operation về `Available`.
- `RescueRequest.Status` giữ nguyên (thường là `Assigned`) cho đến khi Citizen/Guest xác nhận an toàn.

### Quy tắc cho API chuyển sang Waiting

Endpoint:

- `PUT /api/rescue-team/operations/{operationId}/waiting`

Ràng buộc:

- Role: `RESCUE_TEAM`.
- Người gọi phải là thành viên đang hoạt động của team.
- Chỉ cho phép chuyển từ `Assigned` sang `Waiting`.

### Xử lý lỗi

API trả lỗi nghiệp vụ rõ ràng trong các trường hợp:

- Dữ liệu đầu vào không hợp lệ.
- Không tìm thấy operation/request liên quan.
- Sai quyền (không phải Leader hoặc không thuộc team).
- Trạng thái hiện tại không đúng điều kiện chuyển.
- Lỗi lưu dữ liệu ở tầng database.

### Ghi chú quan trọng

- Bước đóng request (`Completed`) là bước cuối cùng của Citizen/Guest.
- Tài liệu này ưu tiên thay đổi tối thiểu để giảm rủi ro ảnh hưởng các luồng khác.
