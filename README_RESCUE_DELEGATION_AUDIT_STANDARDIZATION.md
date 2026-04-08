# Rescue Delegation Audit Standardization

Tài liệu này mô tả chi tiết các thay đổi vừa được áp dụng để chuẩn hóa luồng phân nhiệm vụ cứu hộ theo bảng log mới `rescue_delegation_action_logs`.

## 1) Mục tiêu chuẩn hóa

- Đồng bộ logic API với rule nghiệp vụ trong `Readme_newTable.txt`.
- Mỗi hành động nghiệp vụ map 1-1 với một `action_type`.
- Không để xảy ra trạng thái "update state nhưng không có audit log" trong các thao tác phân nhiệm vụ (ngoại lệ: `accept` theo yêu cầu nghiệp vụ).
- Tất cả thao tác assign/reassign/remove/complete/fail/reject chạy trong transaction.
- Các thao tác nhiều member dùng chung `action_batch_id`.

## 2) File đã thay đổi

- `API/Models/RescueDelegationActionLog.cs` (mới)
- `API/Models/ApplicationDbContext.cs`
- `API/Controllers/RescueTeamController.cs`
- `API/Controllers/RescueOperationController.cs`

## 3) Model và DbContext

### 3.1. Thêm model log mới

Đã thêm entity `RescueDelegationActionLog` map với bảng `rescue_delegation_action_logs`:

- `delegation_action_log_id`
- `action_batch_id`
- `request_id`
- `operation_id`
- `actor_user_id`
- `member_user_id`
- `action_type`
- `action_reason`
- `request_status`
- `operation_status`
- `action_at`

### 3.2. Cập nhật `ApplicationDbContext`

- Thêm `DbSet<RescueDelegationActionLog> RescueDelegationActionLogs`.
- Thêm mapping cột đầy đủ trong `OnModelCreating`.

## 4) Chuẩn hóa endpoint theo nghiệp vụ

## 4.1. `PUT /api/rescue-team/requests/{requestId}/accept`

Giữ nguyên theo yêu cầu:

- Chỉ Leader của team được accept.
- Chỉ accept khi request đang `Verified`.
- Không ghi vào `rescue_delegation_action_logs`.

## 4.2. `PUT /api/rescue-team/requests/{requestId}/reject`

Đã bổ sung đầy đủ:

- Validate `reason` bắt buộc.
- Sinh `action_batch_id` cho toàn bộ thao tác reject.
- Nếu có member active đang giữ request:
  - Ghi `LEADER_REMOVED_MEMBER` cho từng member.
  - Set `member.request_id = null`.
- Ghi `LEADER_REJECTED_REQUEST` kèm `action_reason`.
- Trong cùng transaction:
  - Release vehicle về trạng thái `Available`.
  - `request.status = Verified`
  - `request.team_id = null`
  - cập nhật `request_status_history`.

## 4.3. `POST /api/rescue-team/members/assign-task`

Đã chuyển sang xử lý assign/reassign chuẩn audit:

- Nếu assign mới:
  - Set `member.request_id = requestId`.
  - Ghi `LEADER_ASSIGNED_MEMBER`.
- Nếu reassign (member đang bận request khác):
  - Bắt buộc tìm được operation cũ.
  - Ghi `LEADER_REMOVED_MEMBER` ở operation cũ.
  - Update `member.request_id` sang request mới.
  - Ghi `LEADER_ASSIGNED_MEMBER` ở operation mới.
- Không skip member chỉ vì đang bận (busy -> reassign).
- Dùng chung `action_batch_id` cho toàn request bulk.
- Chạy toàn bộ trong transaction.
- Có bắt lỗi vi phạm unique active assignment để trả về `409 Conflict` dễ hiểu.

## 4.4. `GET /api/rescue-team/my-assignment`

Giữ read-only, không ghi log.

Response được chuẩn lại dữ liệu chính:

- `operationId`
- `requestStatus`
- `operationStatus`

## 4.5. `PUT /api/rescue-team/my-assignment/confirm`

Đã chuẩn hóa đúng rule:

- Validate:
  - user là member active
  - có assignment (`request_id != null`)
  - operation đang `Assigned`
- Trong transaction:
  - set `member.request_id = null`
  - ghi `MEMBER_COMPLETED` (`actor_user_id = member_user_id`)
- Không cập nhật `operation.status`.

## 4.6. `POST /api/rescue-team/my-assignment/support` (endpoint mới)

Đã thêm mới:

- Validate:
  - user là member active
  - có assignment hiện tại
  - operation đang `Assigned`
- Mỗi lần bấm ghi đúng 1 dòng `MEMBER_REQUESTED_SUPPORT`.
- Không gộp nhiều lần bấm.

## 4.7. `PUT /api/rescue-team/operations/{operationId}/status`

Đã chuẩn hóa quyền và thứ tự xử lý:

- Chỉ Leader được complete/fail.
- Hỗ trợ `COMPLETED` và `FAILED`.

Luồng `COMPLETED`:

1. Remove toàn bộ member active trước.
2. Ghi `LEADER_REMOVED_MEMBER` cho từng member.
3. Cập nhật `operation.status = Completed`.
4. Ghi `LEADER_COMPLETED_OPERATION`.
5. Release vehicle về `Available`.

Luồng `FAILED`:

1. Validate `reason` bắt buộc.
2. Remove toàn bộ member active trước + log remove.
3. Cập nhật `operation.status = Failed`.
4. Cập nhật `request.status = Verified`.
5. Ghi `LEADER_FAILED_OPERATION` kèm `action_reason`.
6. Release vehicle về `Available`.

## 4.8. `PATCH /api/RescueOperation/{id}/status`

Đã đồng bộ bằng cách ngừng dùng endpoint cũ trong flow rescue-team:

- Endpoint trả `410 Gone`.
- Message hướng dẫn dùng endpoint chuẩn:
  - `PUT /api/rescue-team/operations/{operationId}/status`

Mục tiêu: tránh lệch logic audit giữa 2 endpoint update status.

## 4.9. `GET /api/rescue-team/members`

Đã mở rộng payload để Leader quyết định phân công tốt hơn:

- `isBusy`
- `currentOperationId`
- `lastActionType`
- `lastAssignedAt`
- `lastSupportRequestedAt`
- `lastCompletedAt`

## 5) Mapping action type đã áp dụng

- `LEADER_ASSIGNED_MEMBER`
- `LEADER_REMOVED_MEMBER`
- `MEMBER_COMPLETED`
- `MEMBER_REQUESTED_SUPPORT`
- `LEADER_COMPLETED_OPERATION`
- `LEADER_FAILED_OPERATION`
- `LEADER_REJECTED_REQUEST`

## 6) Quy ước batch và transaction

### 6.1. `action_batch_id`

- Mỗi thao tác bulk/business action lớn sinh 1 `Guid`.
- Tất cả dòng log phát sinh trong cùng thao tác dùng chung batch:
  - reject request
  - assign/reassign nhiều member
  - complete/fail operation có remove nhiều member

### 6.2. Transaction boundary

Các endpoint có thay đổi assignment/log đều được bọc transaction:

- `reject`
- `assign-task`
- `my-assignment/confirm`
- `operations/{operationId}/status`

Mục tiêu: không tồn tại trạng thái cập nhật dở dang giữa data chính và audit log.

## 7) Chuẩn hóa trạng thái vehicle

Đã thống nhất cập nhật vehicle release về `Available` để khớp check constraint DB (không phụ thuộc hoa/thường khi so sánh).

## 8) Kết quả build và kiểm tra

- `dotnet build`: thành công.
- Không phát sinh error compile từ phần thay đổi.
- Còn warning cũ ở `RescueRequestController` (không thuộc phạm vi chỉnh sửa lần này).

## 9) Gợi ý checklist test nhanh (QA/API)

- Reject có reason:
  - Có member active -> có log remove từng member + log reject cùng batch.
- Assign mới:
  - member rảnh -> có `LEADER_ASSIGNED_MEMBER`.
- Reassign:
  - member bận request khác -> có remove cũ + assign mới cùng batch.
- Member confirm:
  - set `request_id = null`, có `MEMBER_COMPLETED`, operation không đổi status.
- Member support:
  - bấm 2 lần -> có 2 dòng `MEMBER_REQUESTED_SUPPORT`.
- Leader complete:
  - remove active members trước, sau đó log completed operation.
- Leader fail:
  - bắt buộc reason, request quay `Verified`, có log failed operation.
- Gọi endpoint `PATCH /api/RescueOperation/{id}/status`:
  - nhận `410 Gone`.
