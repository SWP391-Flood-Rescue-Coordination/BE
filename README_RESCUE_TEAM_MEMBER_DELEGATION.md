# 📘 Rescue Team - Tính năng Giao Việc (Task Delegation)

Tài liệu này giải thích cách hoạt động của quy trình phân công nhiệm vụ từ **Leader (Đội trưởng)** xuống **Member (Thành viên)** bên trong một Đội cứu hộ, dựa trên luồng dữ liệu mới nhất.

## 📌 Khái niệm cốt lõi: Quản lý Rảnh/Bận bằng `request_id`

Bảng `rescue_team_members` **KHÔNG** sử dụng cột `status` để theo dõi trạng thái. Việc một thành viên đang "rảnh" hay "bận" được xác định duy nhất qua cột `request_id`:
- `request_id IS NULL`: Thành viên đang **Rảnh (Available)**, sẵn sàng nhận nhiệm vụ mới.
- `request_id IS NOT NULL`: Thành viên đang **Bận (Busy)** thực hiện nhiệm vụ có ID tương ứng.

---

## 📡 Danh sách Endpoints mới

### 1. Leader: Từ chối phân công (Reject Request)
```
PUT /api/rescue-team/requests/{requestId}/reject
```
- **Quyền hạn:** User phải có role `RESCUE_TEAM`, đồng thời bản ghi trong `rescue_team_membe`membrs` phải có er_role = 'Leader'`.
- **Hoạt động:** 
  - Nếu Leader thấy lệnh phân công không phù hợp, họ có quyền Từ chối.
  - Hệ thống tự động gỡ `TeamId` khỏi Yêu cầu.
  - Trạng thái Yêu cầu đổi từ `Assigned` về `Verified`.
  - Toàn bộ Phương tiện đã lỡ điều phối sẽ được trả về trạng thái `AVAILABLE`.

### 2. Leader: Giao nhiệm vụ cho Thành viên (Assign Task)
```
POST /api/rescue-team/members/assign-task
```
- **Quyền hạn:** `Leader`.
- **Request Body:**
  ```json
  {
      "userIds": [2, 4, 8],
      "operationId": 15
  }
  ```
- **Hoạt động:** 
  - Hệ thống lặp qua danh sách `userIds`.
  - Nếu thành viên nhận lệnh đang Rảnh (`request_id == null`), hệ thống sẽ gán `request_id` của Operation vào cho họ, đánh dấu là Bận.
  - Những ID nào không thuộc đội hoặc đang bận sẽ bị bỏ qua (Skip) nhưng không làm hỏng toàn bộ request.
  - API trả về danh sách `assignedUserIds` và `skippedUserIds` để Frontend dễ kiểm soát.

### 3. Member: Kiểm tra Nhiệm vụ cá nhân (Get My Assignment)
```
GET /api/rescue-team/my-assignment
```
- **Quyền hạn:** `RESCUE_TEAM`.
- **Hoạt động:**
  - Nếu Member bị Leader gán `request_id`, API sẽ trả về toàn bộ chi tiết Nhiệm vụ (Tọa độ, Báo cáo, Thời gian, Tên Đội).
  - Nếu Member đang Rảnh (`request_id = null`), API sẽ trả về lỗi `404 Not Found` kèm thông điệp báo Rảnh để ngăn chặn / chặn lại việc xem thông tin rác.
  - Đây là endpoint dành riêng cho các thành viên bình thường xem đúng 1 Task mà bản thân phải chịu trách nhiệm.

### 4. Member: Xác nhận hoàn tất nhiệm vụ (Confirm Task) ✅
```
PUT /api/rescue-team/my-assignment/confirm
```
- **Quyền hạn:** Tài khoản có role `RESCUE_TEAM`, đồng thời bản ghi trong `rescue_team_members` phải có `member_role = 'Member'`.
- **Request Body:** Không cần (No Body).
- **Hoạt động:**
  - Hệ thống tự động lấy `UserId` từ JWT Token.
  - **Kiểm tra vai trò (Database):** Nếu người dùng là `Leader`, API sẽ báo lỗi `403 Forbidden` (Đội trưởng quản lý nhiệm vụ qua API Status chung, không thực hiện xác nhận cá nhân tại đây).
  - **Kiểm tra trạng thái:** Nếu Member đang được giao việc (`request_id != null`) và Operation liên quan đang ở trạng thái `Assigned`.
  - **Giải phóng:** Hệ thống đặt `request_id = null` cho Member này → Trạng thái trở về **Rảnh (Available)**, sẵn sàng nhận Task tiếp theo từ Leader.
- **Mã lỗi phản hồi:**
  - `200 OK`: Xác nhận thành công. Trả về `operationId` và `requestId`.
  - `403 Forbidden`: Người dùng là Leader, không được dùng quyền của Member.
  - `404 Not Found`: Không tìm thấy nhiệm vụ hoặc Member đang rảnh.
  - `400 Bad Request`: Operation không ở trạng thái `Assigned` để xác nhận.

### 5. Leader: Xem và tìm kiếm danh sách Thành viên (Get Team Members)
```
GET /api/rescue-team/members?search={keyword}
```
- **Quyền hạn:** `Leader`.
- **Hoạt động:**
  - Trả về danh sách toàn bộ thành viên đang thuộc quyền quản lý của Leader.
  - Hỗ trợ tham số `search` trên query string để tìm kiếm theo: **Tên (FullName), Tên tài khoản (Username), Số điện thoại (Phone), hoặc ID (UserId)**.
  - Output cung cấp field `isBusy: boolean` (được nội suy từ giá trị `request_id != null`) giúp Frontend dễ dàng hiển thị nhãn "Rảnh" hoặc "Bận".

---

## 🔄 Luồng Tự Động Thu Hồi / Giải Phóng Thành Viên

Để tránh tình trạng thành viên bị "kẹt" vĩnh viễn ở trạng thái Bận (`request_id != null`), hệ thống có các cơ chế tự động giải phóng (Set `request_id = null`):

1. **Khi nhiệm vụ hoàn thành / thất bại (Mức Operation):**
   - API `PUT /api/rescue-operations/operations/{operationId}/status` cập nhật Operation thành `COMPLETED` học `FAILED`.
   - Hệ thống quét tất cả thành viên đang giữ `requestId` của operation này và trả lại `null`.

2. **Khi nhiệm vụ hoàn thành / thất bại (Mức Team):**
   - API `PUT /api/rescue-team/my-mission/status` (hoặc chức năng cập nhật status tương tự của Team) khi kết thúc quy trình cũng sẽ tự động reset `request_id` của các members tham gia.

Mọi thứ hoàn toàn tự động, tránh việc Leader phải tự tay đi xóa `request_id` cho từng Member 1 cách thủ công!
