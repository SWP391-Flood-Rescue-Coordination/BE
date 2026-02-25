# 📘 Rescue Team - Cập nhật trạng thái nhiệm vụ

## Mục đích
API này cho phép thành viên **Rescue Team** cập nhật trạng thái thực hiện nhiệm vụ cứu hộ theo thời gian thực. Hệ thống tự động đồng bộ trạng thái sang yêu cầu cứu hộ, ghi lịch sử, và giải phóng tài nguyên (đội + phương tiện) khi hoàn thành.

---

## 🔐 Xác thực & Phân quyền

| Vai trò | Quyền truy cập |
|---|---|
| `RESCUE_TEAM` | ✅ Toàn quyền với 2 endpoint bên dưới |
| Các vai trò khác | ❌ 403 Forbidden |

**Cách lấy token:**
1. Gọi `POST /api/Auth/login` với tài khoản có `role = RESCUE_TEAM`.
2. Lấy `accessToken` từ response.
3. Trong Swagger: nhấn nút **Authorize 🔒** → nhập `Bearer <accessToken>`.

---

## 📡 Danh sách Endpoints

### 1. Xem nhiệm vụ được phân công

```
GET /api/rescue-team/my-assignments
```

Trả về danh sách các nhiệm vụ đang chờ xử lý (status: `ASSIGNED`, `EN_ROUTE`, `ARRIVED`) của đội mà user đang tham gia.

**Không cần tham số gì.** Hệ thống tự xác định đội dựa vào JWT token.

**Response mẫu:**
```json
{
  "success": true,
  "total": 1,
  "data": [
    {
      "assignmentId": 5,
      "requestId": 12,
      "requestTitle": "Cần cứu hộ tại khu vực A",
      "requestStatus": "ASSIGNED",
      "requestAddress": "123 Đường Lê Lợi, Q.1",
      "requestLatitude": 10.77609800,
      "requestLongitude": 106.70082300,
      "teamName": "Đội Cứu hộ 01",
      "vehicleName": "Xuồng cứu hộ số 3",
      "status": "ASSIGNED",
      "assignedAt": "2026-02-25T06:00:00Z",
      "startedAt": null
    }
  ]
}
```

---

### 2. Cập nhật trạng thái nhiệm vụ ⭐

```
PUT /api/rescue-team/assignments/{assignmentId}/status
```

**Path Parameter:**
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `assignmentId` | `int` | ✅ | ID của nhiệm vụ (assignment) cần cập nhật |

**Request Body (JSON):**
```json
{
  "newStatus": "IN_PROGRESS",
  "expectedCurrentStatus": "ASSIGNED",
  "notes": "Đội đang trên đường đến hiện trường"
}
```

| Field | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `newStatus` | `string` | ✅ | Trạng thái mới: `"IN_PROGRESS"` hoặc `"COMPLETED"` |
| `expectedCurrentStatus` | `string` | ✅ | Trạng thái hiện tại mà client đang thấy (dùng để kiểm tra concurrency) |
| `notes` | `string` | ❌ | Ghi chú thêm (tùy chọn) |

---

## 🔄 Luồng chuyển trạng thái

```
rescue_assignments:   ASSIGNED  ──►  EN_ROUTE  ──►  COMPLETED
                                        ↑  
                      (khi newStatus = "IN_PROGRESS")

rescue_requests:      ASSIGNED  ──►  IN_PROGRESS  ──►  COMPLETED
```

| `newStatus` gửi lên | `rescue_assignments.status` thay đổi thành | `rescue_requests.status` thay đổi thành |
|---|---|---|
| `IN_PROGRESS` | `EN_ROUTE` | `IN_PROGRESS` |
| `COMPLETED` | `COMPLETED` | `COMPLETED` |

---

## 🚦 Business Logic

### Kiểm tra trước khi cập nhật

| # | Điều kiện | Lỗi trả về |
|---|---|---|
| 1 | User phải là thành viên **active** của team được phân công | `403 Forbidden` |
| 2 | `rescue_requests.status` phải là `ASSIGNED` hoặc `IN_PROGRESS` | `400 Bad Request` |
| 3 | `newStatus` chỉ được là `IN_PROGRESS` hoặc `COMPLETED` | `400 Bad Request` |
| 4 | Transition hợp lệ theo bảng ở trên | `400 Bad Request` |
| 5 | `expectedCurrentStatus` phải khớp với DB | `409 Conflict` |

### Hành động sau khi cập nhật thành công

| Sự kiện | Hành động |
|---|---|
| Chuyển sang `IN_PROGRESS` | Ghi `started_at = NOW()` vào `rescue_assignments` |
| Chuyển sang `COMPLETED` | Ghi `completed_at = NOW()` vào `rescue_assignments` |
| Bất kỳ cập nhật nào | Ghi `updated_at = NOW()` vào `rescue_requests` |
| Bất kỳ cập nhật nào | Thêm 1 record mới vào `rescue_request_status_history` |
| Chuyển sang `COMPLETED` | `rescue_teams.status` → `AVAILABLE` |
| Chuyển sang `COMPLETED` | `vehicles.status` → `AVAILABLE` (nếu có xe được phân công) |

---

## 🔒 Concurrency Handling (Xử lý tranh chấp)

Hệ thống dùng **optimistic concurrency** để tránh trường hợp 2 thành viên cùng cập nhật một nhiệm vụ cùng lúc.

**Cách hoạt động:**
- Client phải gửi `expectedCurrentStatus` = trạng thái hiện tại mà họ đang nhìn thấy.
- Server so sánh `expectedCurrentStatus` với giá trị trong DB trước khi cập nhật.
- Nếu không khớp → Server trả về **`409 Conflict`**.

**Response 409 mẫu:**
```json
{
  "success": false,
  "message": "Xung đột trạng thái (Concurrency): Trạng thái hiện tại là 'EN_ROUTE', bạn đang kỳ vọng 'ASSIGNED'. Vui lòng tải lại và thử lại.",
  "currentStatus": "EN_ROUTE"
}
```

**Cách xử lý ở phía client:** Đọc `currentStatus` trong response 409, cập nhật lại giao diện, rồi gửi lại request với `expectedCurrentStatus` mới.

---

## 📋 Response mẫu (Thành công)

**Bắt đầu nhiệm vụ (IN_PROGRESS):**
```json
{
  "assignmentId": 5,
  "requestId": 12,
  "assignmentStatus": "EN_ROUTE",
  "requestStatus": "IN_PROGRESS",
  "startedAt": "2026-02-25T12:30:00Z",
  "completedAt": null,
  "message": "Bắt đầu thực hiện nhiệm vụ thành công."
}
```

**Hoàn thành nhiệm vụ (COMPLETED):**
```json
{
  "assignmentId": 5,
  "requestId": 12,
  "assignmentStatus": "COMPLETED",
  "requestStatus": "COMPLETED",
  "startedAt": "2026-02-25T12:30:00Z",
  "completedAt": "2026-02-25T14:15:00Z",
  "message": "Hoàn thành nhiệm vụ thành công. Đội và phương tiện đã được giải phóng."
}
```

---

## 🧪 Hướng dẫn test trên Swagger

1. Truy cập `http://localhost:5188/swagger`
2. Gọi `POST /api/Auth/login` với tài khoản Rescue Team → copy `accessToken`
3. Nhấn **Authorize 🔒** → nhập `Bearer <accessToken>` → **Authorize**
4. **Bước 1:** Gọi `GET /api/rescue-team/my-assignments` → Ghi nhớ `assignmentId` và `status`
5. **Bước 2:** Gọi `PUT /api/rescue-team/assignments/{assignmentId}/status` với body:
   ```json
   {
     "newStatus": "IN_PROGRESS",
     "expectedCurrentStatus": "ASSIGNED",
     "notes": "Test bắt đầu nhiệm vụ"
   }
   ```
6. **Bước 3:** Gọi lại với `newStatus = "COMPLETED"` và `expectedCurrentStatus = "EN_ROUTE"`

---

## ⚠️ Các lỗi thường gặp

| HTTP Code | Nguyên nhân | Cách khắc phục |
|---|---|---|
| `401 Unauthorized` | Chưa đăng nhập hoặc token hết hạn | Đăng nhập lại để lấy token mới |
| `403 Forbidden` | Không phải thành viên active của team | Kiểm tra tài khoản có trong `rescue_team_members` với `is_active = 1` |
| `400 Bad Request` | Trạng thái không hợp lệ hoặc sai transition | Đọc message trong response để biết trạng thái hiện tại |
| `404 Not Found` | `assignmentId` không tồn tại | Kiểm tra lại ID nhiệm vụ |
| `409 Conflict` | Trạng thái đã bị thay đổi bởi người khác | Đọc `currentStatus` trong response, cập nhật `expectedCurrentStatus` rồi gửi lại |

---

*File được tạo tự động - Ngày: 25/02/2026*
