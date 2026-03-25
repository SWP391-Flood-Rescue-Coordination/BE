# Phân Tích Kỹ Thuật Toàn Diện Lõi Dự Án Flood Rescue Coordination

Tài liệu này đi sâu vào phân tích các đoạn mã (code snippets) của **TẤT CẢ CÁC HÀM** trong hệ thống. Cấu trúc mô phỏng theo định dạng: `[Trích đoạn Code quan trọng] -> Gạch đầu dòng phân tích`.

---

# PHẦN 1: TẦNG CONTROLLERS (API Endpoints)

## 1. AuthController (Kiểm soát đăng nhập, đăng ký)

### 1.1 `Login` & `Register`
```csharp
var response = await _authService.LoginAsync(request);
if (!response.Success) return Unauthorized(response);
return Ok(response);
```
* **Clean Code**: Thay vì xử lý logic kết nối DB ở Controller, Controller chỉ nhận biến đầu vào `[FromBody]` và ném thẳng sang `_authService`.
* **Phân rã Http Status Code**: Nếu Service trả về False, lập tức bọc trong hàm `Unauthorized` (Lỗi 401) hoặc `BadRequest` (Lỗi 400). Nếu trả về True thì gói trong `Ok` (Lỗi 200). Đảm bảo chuẩn REST API.

### 1.2 `RefreshToken`
```csharp
var response = await _authService.RefreshTokenAsync(refreshToken);
```
* **Mục đích**: Nhận chuỗi Token đã cũ từ Frontend, chuyển cho Service để xin gia hạn mã JWT mới mà không cần bắt user phải đăng nhập lại.

### 1.3 `Logout`
```csharp
var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
var response = await _authService.LogoutAsync(accessToken, refreshToken);
```
* **Bóc tách Header**: Lấy mã Token trực tiếp từ Header `Authorization` được gửi ngầm từ client, bóc mất chữ `Bearer ` để lấy lõi Token mang đi khóa (Blacklist).

### 1.4 `GetCurrentUser`
```csharp
var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
```
* **Context Reading**: Đọc thông tin đã được giải mã sẵn từ JWT của người dùng (từ biến `User`) mà không cần tốn lệnh gọi xuống Database.

### 1.5 `SendForgotPasswordOtp` & `ResetPassword`
```csharp
var response = await _authService.SendForgotPasswordOtpAsync(request);
```
* **Mở luồng vô danh**: Hàm này gọi ra để gửi tin nhắn yêu cầu mã OTP kích hoạt đến số điện thoại quên mật khẩu. Không sử dụng thẻ gác cổng `[Authorize]` ở đây vì người khôi phục mật khẩu chưa đăng nhập được.

---

## 2. RescueRequestController (Yêu cầu cứu hộ từ dân)

### 2.1 `CreateRequest` (Tạo yêu cầu)
```csharp
isDuplicate = await _context.RescueRequests.AnyAsync(r =>
    r.CreatedAt >= DateTime.UtcNow.AddMinutes(-15) &&
    (r.Phone == checkPhone || r.ContactPhone == checkPhone) &&
    r.Address.Trim().ToLower() == normalizedAddress);

var priorityScore = 1.5 * elderly + 1.8 * children;
// ... điểm cộng thêm nếu keyword "sập nhà", "ngập 1m"...
```
* **Chống Spam/Trùng lặp**: Cùng một người dùng, SĐT, Địa chỉ nếu xin cứu viện liên tục trong 15 phút thì yêu cầu sau bị đập cờ `Duplicate` thay vì `Pending`.
* **Thuật toán Độ Ưu Tiên**: Áp dụng trọng số lên người già/mẫu giáo. Kết hợp rà soát keywords khẩn cấp để tự cộng điểm, tính ra hạng khẩn cấp `PriorityLevelId`.

### 2.2 `GetMyRequests`, `GetMyLatestRequest`, `GetAllRequests`
```csharp
if (!string.IsNullOrEmpty(searchTerm))
{
    var term = searchTerm.Trim().ToLower();
    query = query.Where(r => r.Title.ToLower().Contains(term) || r.Address.ToLower().Contains(term) ... );
}
requests = requests
    .OrderByDescending(r => r.Status == "Pending" || r.Status == "Verified")
    .ThenBy(r => GetWardFromAddress(r.Address))
    .ThenByDescending(r => r.CreatedAt).ToList();
```
* **Full-text Search Mềm (`searchTerm`)**: Mới được cập nhật để cho phép Admin gõ tìm kiếm quét qua mọi trường văn bản (Tên, SĐT, Địa chỉ, ID).
* **Phân quyền truy xuất**: Dân thường chỉ lấy ID theo Token `User.FindFirst`. Admin được lấy danh sách toàn bộ bằng `.AsQueryable()`.
* **Sắp xếp Đa Tầng (Multi-tier Sort)**: Ưu tiên nhồi các cụm đang chờ (`Pending/Verified`) lên cao nhất -> Gom các khu vực phường xã vùng gần nhau (`GetWardFromAddress`) -> Mới nhất xếp trên cùng.

### 2.9 `GetAllRequestStatusHistories` (Mới Cập Nhật)
```csharp
UpdatedByName = h.UpdatedBy == -1 ? "GUEST" : _context.Users...
```
* **Nhật ký Admin**: Trả về toàn bộ lịch sử chuyển đổi trạng thái của yêu cầu. Xử lý triệt để Identity bằng cách gán cứng `-1` dịch ra thành chữ `GUEST` để hiển thị Dashboard.

### 2.3 `GetWardFromAddress` (Helper)
```csharp
if (p.StartsWith("phường") || p.StartsWith("p.") || p.StartsWith("xã") || p.StartsWith("thị trấn"))
    return p;
```
* **Tách Data**: Viết một vòng lặp nhỏ để mò trong cấu trúc địa chỉ có dấu "phẩy". Lấy ra phân khu cụm xã/phường phục vụ cho việc nhóm các khu này cho trực thăng bay chung 1 tốp.

### 2.4 `GetRequestById` & `GetRequestByIdForGuest`
```csharp
// Guest Endpoint thì cho thông số NULL vào:
CitizenName = ..., CitizenPhone = ...
```
* **Ẩn danh (Annonymouse Data)**: API dành cho Guest không cần Authentication (Không có thẻ `[Authorize]`), nhưng hàm chọn `.Select()` sẽ tự động giấu bớt thông tin nhạy cảm của người duyệt.

### 2.5 `UpdateRequestByGuest` & `UpdateRequestByCitizen`
```csharp
if (request.Status != "Pending" && request.Status != "Verified" && request.Status != "Duplicate")
    return BadRequest("...Không thể sửa");
```
* **Chặn luồng (Status Lock)**: Một khi cứu hộ đã "Đang đi" hoặc xe lăn bánh (In Progress), dân tuyệt đối không được sửa địa chỉ yêu cầu nữa để tránh chết người vì xe đi nhầm chỗ. Cập nhật sẽ tái chạy hàm tính lại Ưu Tiên và Trùng Lặp một lần nữa.

### 2.6 `UpdateStatus` & `VerifyRequest`
```csharp
_context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory { ... });
```
* **Ghi vết Sự Cố**: Bất kỳ khi quản lý đổi trạng thái hoặc duyệt đơn, ngoài bảng gốc `RescueRequests`, hệ thống luôn Log lại một bản Audit Trail vào `RescueRequestStatusHistories` để đổ trách nhiệm ai duyệt sai.

### 2.7 `ConfirmRescued` & `GuestConfirmRescued`
```csharp
var canReportSafe = await RequestHasCompletedOperationAsync(request.RequestId);
if (!canReportSafe) return BadRequest("Doi cuu ho chua confirm...");
```
* **Nghiệm thu (End-of-life)**: Công dân chỉ được phép ấn nút "Tôi đã an toàn" nếu như trên thực tế Đội Quản lý báo là lệnh ` RescueOperation` đã chạy xong. Dành riêng phần Guest phải truyền SĐT đúng thì mới cho tự nghiệm thu.

### 2.8 Các hàm Helper đính kèm: `ApplyCanReportSafeAsync`, `RequestHasCompletedOperationAsync`, vân vân
* **Check chéo**: Check trong Database xem Request này đã từng có Đội Cứu Hộ hoàn thành Operation chưa, để kích hoạt property `CanReportSafe = true` lên UI render cái còi cấp cứu thành nút xanh báo an toàn.

---

## 3. RescueOperationController (Điều phối nhiệm vụ & Xe kéo)

### 3.1 `AssignRescue` (Gán nhiệm vụ - RẤT QUAN TRỌNG)
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();

// Tạo operation
_context.RescueOperations.Add(new RescueOperation ...);

// Cập nhật yêu cầu, Đội xe, history
rescueRequest.Status = "Assigned";
rescueTeam.Status = "BUSY";
vehicles.Status = "InUse";

await _context.SaveChangesAsync();
await transaction.CommitAsync();
```
* **Transaction An Toàn Tuyệt Đối**: Quá trình tạo công việc này cập nhật 5 bảng cùng một lúc. Mọi sự thay đổi phải gói vào thẻ `BeginTransactionAsync`. Nếu điện rớt mạng giữa chừng, thao tác sẽ "hủy toàn bộ" (Rollback) chứ không gây nên lỗi lệch bộ nhớ (Rác).
* **Validation mạnh**: Kiểm tra ngặt nghèo xem Đội đó có đang ở trạng thái Available không? Xe cộ được đánh dấu (tick) có còn ở trạm không hay đã vào vùng lũ `InUse` rồi.

### 3.2 `GetOperationsByTeam` & `GetOperationById`
```csharp
var isMember = await _context.RescueTeamMembers
    .AnyAsync(m => m.TeamId == teamId && m.UserId == currentUserId && m.IsActive);
```
* **Bảo Mật Truy Cập**: Team nào chỉ được xem Operation của team đấy. Kiểm tra xem người đang đăng nhập có thực sự thuộc Team (bảng `RescueTeamMembers` + `IsActive == true`) không mới cấp dữ liệu.

### 3.3 `UpdateOperationStatus`
```csharp
// Giống hệt Team Controller nhưng được cấp cho nhóm cấp độ quản lý can thiệp thẳng.
```
* **Mở khóa Xe (Release lock)**: Nếu Status được đá thành `Completed`/`Failed`, vòng lặp sẽ gỡ `InUse` khỏi toàn bộ nhóm siêu xe trực thăng và trả chúng và Team về trạng thái `AVAILABLE`.

### 3.4 `GetNearestTeamsForRequest`
```csharp
var distanceKm = await _distanceService.GetRoadDistanceKmAsync(...)
```
* **Bắn API định tuyến thực**: Dùng Vĩ độ Kinh độ của Đội và Người cần cứu để yêu cầu Service tính khoảng cách *ĐƯỜNG BỘ LÁI XE* chân thực, sort từ gần nhất lên đầu.

---

## 4. RescueTeamController (Tính năng của Ứng dụng lính cứu hộ)

### 4.1 `UpdateMissionStatus`
```csharp
catch (DbUpdateException ex) when (IsDuplicateRequestStatusHistoryError(ex)) { ... }
```
* **Nghiệp vụ Xóa nợ / Hủy khẩn cấp**: Lính dưới rốn lũ bấm App xác nhận thành công hay thất bại (Kẹt đường sạt lở bắt buộc có lý do `dto.Reason`). Nếu thất bại, đơn cầu cứu sẽ hoàn về trạng thái `Verified` để máy chủ gọi đội khác mạnh hơn.
* **Chống Ghi Đúp (Anti-concurrency)**: Bắt lỗi cập nhật đồng thời nhiều app một lúc tạo ra xung đột khóa với catch `IsDuplicateRequestStatusHistoryError`.

### 4.2 `GetMyOperations`, `GetMissionDetails`, `GetTeamsWithStatus`
* Giống phần lấy dữ liệu ở Operation, nhưng hàm `.Select` đổ trả về cục Data có chứa mảng `VehicleIds` và string kết nối thông qua `.Join` DB.

---

## 5. StockHistoryController (Log tồn kho vật tư)

### 5.1 `GetStockHistory`
* **Lọc log In/Out**: Lọc nhật ký `IN` (nhập), `OUT` (xuất) bằng `OrderByDescending` trả về mới nhất trước.

### 5.2 `ImportStock` & `ExportStock`
```csharp
reliefItem.Quantity -= itemInput.Quantity;
// ...
var history = new StockHistory { Type = "OUT", Date = DateTime.UtcNow, Body = bodyContent };
_context.StockHistories.Add(history);
```
* **Bảo vệ hàng tồn kho**: Ở chiều Xuất kho (Export), check hàm `item.Quantity < itemInput.Quantity` (Kiểm tra xem kho có đủ mì tôm / phao không).
* **Viết Log định tuyến (Audit Trail)**: Mọi thao tác đều bắt buộc trừ/cộng lượng thực tế trong bảng tổng, và chèn một dòng vào bảng `StockHistories` mô tả rất rõ bằng Serialization dạng chuỗi (`Body = "12-10, 13-5"`) để kế toán truy vết. Vẫn dùng `Transaction` cực chắc tay.

---

## 6. ReliefItemController (Quản trị danh sách hàng)

### 6.1 `GetAllReliefItems` & `UpdateReliefItem`
* **CRUD cơ bản**: Cho phép Admin truy xuất và cấu hình tên mặt hàng, số lượng TỐI THIỂU `MinQuantity`.

### 6.2 `GetLowStockItems` & `CountLowStockItems`
```csharp
var items = await _context.ReliefItems.Where(i => i.Quantity <= n)
```
* **Cảnh báo Cạn Kiệt**: Filter trực tiếp nạp thẳng tham số `n`. Kho còn hụt dưới n thì đổ chuông cảnh báo thiếu phao vớt.

---

## 7. VehicleController (Quản lý xe và trực thăng)

### 7.1 `GetAllVehicles`, `GetVehicleById`, `UpdateVehicle`
```csharp
var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };
if (!manualValidStatuses.Contains(newStatus)) return BadRequest(...);
if (currentStatus == "INUSE" && currentStatus != newStatus) return BadRequest(...);
```
* **Khóa trạng thái (Status Lock)**: Một bản cập nhật mới cấm Admin gõ trạng thái bậy bạ. Việc gán trạng thái `INUSE` bị cấm thủ công vì nó phải do `RescueOperation` điều phối bằng `Transaction`. 
* **Theo dõi vị trí (Coordinates)**: Mở thêm biến `Latitude` và `Longitude` vào hàm để vẽ tọa độ trực thăng/cano lên bản đồ.
* **Xử lý FK**: Trong logic update, trả về JSON `Include(v => v.VehicleType)` để truy xuất rõ tên loại xe.

### 7.2 `CreateVehicle` & `DeleteVehicle` (Mới Cập Nhật)
```csharp
if (vehicle.Status.ToUpper() == "INUSE") return BadRequest("Không thể xóa phương tiện khi đang làm nhiệm vụ...");
var historyRecords = _context.RescueOperationVehicles.Where(rov => rov.VehicleId == id);
_context.RescueOperationVehicles.RemoveRange(historyRecords);
```
* **Cơ chế Soft-block**: Tuyệt đối cấm thao tác xóa xe khỏi DB nếu xe này đang chở lính đi làm nhiệm vụ (`INUSE`). 
* **Dọn dẹp mồ côi (Cascade Delete)**: Trước khi xóa `vehicle`, hệ thống dọn dẹp các Record tham chiếu ở bảng `RescueOperationVehicles` nhằm tránh văng lỗi khóa ngoại (Data Integrity).

---

## 8. UserInfoController (Quản lý Quyền Lập Trình Viên và Nhân Viên)

### 8.1 `GetAllUsers`, `GetAvailableRoles`
* Chức năng hiển thị dạng bảng Dashboard.

### 8.2 `UpdateUserRole`
```csharp
if (user.Role == "ADMIN" || user.Role == "MANAGER")
    return BadRequest("Admin không thể thay đổi role của cấp quản lý");
```
* **Bảo vệ Hệ Thống**: Tuyệt đối Admin không thể trượt tay hạ quyền 1 Admin cao nhất khác, cũng không được tự bấm tước quyền Role của chính ID bản thân. 

### 8.3 `UpdateUserStatus`
* Cấm cá nhân Admin có token tự gạch mình kích tài khoản về vô hiệu lực (Self-ban Prevention).

---
---

# PHẦN 2: TẦNG SERVICES (Xử lý Nhiệm Vụ Độc Lập)

## 1. AuthService (Nghiệp vụ Mã Hóa & OTP)

### 1.1 `LoginAsync`
```csharp
var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
var user = await _context.Users.FirstOrDefaultAsync(u => phoneCandidates.Contains(u.Phone));

if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return Error...
```
* **Tolerant (Khoan dung đầu vào SĐT)**: Vì DB SĐT Việt gõ rất lằng nhằng (09xx, +84, 84). Hàm sinh ra cả 3 mẫu biến thể và cho lọt `Contains` để tìm.
* **So khớp Băm (Hashing)**: Không hề lưu pass, dùng thuật toán BCrypt cường độ chống Brute force cao để băm ngược (Verify). 

### 1.2 `RegisterAsync`
```csharp
var loginResponse = await LoginAsync(new LoginRequest { Phone = normalizedPhone ... });
if (loginResponse.Success) loginResponse.Message = "Đăng ký thành công";
```
* **Hành vi (Auto-Login)**: Lưu dữ liệu công dân (kèm BCrypt Hash). Đỉnh cao là việc sử dụng phương thức "Triệu hồi bản thân" gọi ngược hàm `LoginAsync` lấy token về. Giết 2 con chim 1 mũi tên, người dùng đăng ký xong được phát nguyên khóa Token để xài app tức khắc.

### 1.3 `RefreshTokenAsync`
```csharp
storedToken.RevokedAt = DateTime.UtcNow;
var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
var newRefreshToken = _jwtService.GenerateRefreshToken();
```
* **Refresh Token Rotation (Chuẩn bảo mật cao cấp)**: Thu hồi Refresh Token cũ đưa vào dĩ vãng (Khóa chốt ngày RevokedAt). Liên tục đẻ ra cặp Token hoàn toàn khác để app tiếp tục sống hạn trong hàng tháng trời mà kẻ cắp không thể trộm token cũ đem gửi nhờ.

### 1.4 `LogoutAsync`
* Lấy thẻ Access Token đập ngược vào bảng `BlacklistedTokens`. Xóa nợ Refresh Token. Hệ thống từ biệt Session sạch sẽ.

### 1.5 Cụm OTP `SendForgotPasswordOtpAsync` & `ResetPasswordWithOtpAsync`
* Kết nối giao tiếp Interface qua luồng gửi SMS giả lập. Nhận OTP test `123456`. Ghi đè mật khẩu băm lên database. 

---

## 2. JwtService (Sản xuất và Giải mã Chữ Ký Token)

### 2.1 `GenerateAccessToken` 
```csharp
var claims = new[] {
    new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
    new Claim(ClaimTypes.Role, user.Role),
};
```
* Đóng gói mã băm JWT bằng cấu trúc `SymmetricSecurityKey` và thuật toán `HmacSha256` bằng khóa Secret. Trộn quyền (Role) lẫn ID vào nội tạng chuỗi để gửi.

### 2.2 `GenerateRefreshToken` 
* Quăng xúc xắc hệ điều hành `RandomNumberGenerator` cắt 32 bytes ngẫu nhiên vô tận nhồi chuẩn `Base64` để làm thẻ bài lưu Token gia hạn.

### 2.3 `GetTokenExpiration` & `GetPrincipalFromExpiredToken`
```csharp
var tokenValidationParameters = new TokenValidationParameters {
    ValidateIssuerSigningKey = true,
    ValidateLifetime = false // Bỏ qua bắt lỗi TimeExp 
};
```
* **Bypass Validation (Khuất tất an toàn)**: Nhằm mục đích phục vụ thao tác làm mới Token trên Frontend. Thư viện JWT khi Validate chữ cái Expire sẽ báo văng lỗi. Hàm này chèn cờ `ValidateLifetime = false` "Mày bỏ qua giới hạn thời gian đi, chỉ check nó phải là hàng thật không giả mạo chữ ký là được". Thế là giải nén được Token ra đọc đàng hoàng.

---

## 3. Các Services Địa Lý & SMS (External Apis)

### 3.1 `OsrmDistanceService` (GetRoadDistanceKmAsync)
```csharp
var url = $"route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false";
var response = await _httpClient.GetFromJsonAsync<OsrmRouteResponse>(url);
return route.Distance / 1000.0;
```
* **Logic Routing Trực Tiếp**: Gửi gói tin REST lấy thẳng đường đi qua OSRM (Bản đồ thế giới thực chạy phương tiện lốp xe `driving`). Dữ liệu trả về từ Json được chuyển ngữ bằng `System.Text.Json` và chia tỷ lệ ngàn lấy ra chuẩn đo KM đường bộ thực tế (Khác biệt đường chim bay thẳng).

### 3.2 `NominatimGeocodingService` (ReverseGeocodeAsync)
* Đẩy tọa độ lên API Nominatim để nó dò map trả thành địa chỉ chữ cho người mù công nghệ hiểu (Địa chỉ nhà, Tên xã phường).

### 3.3 `MockSmsService` (SendOtpAsync & VerifyOtpAsync)
* Thay thế cho `ISmsService`. Vì host hệ thống không có thẻ hay mua gói SMS tốn tiền. App chèn cờ Log trên Console kèm Constant Magic Number `123456` để qua ải. Giải quyết độ tin cậy khi test liên tục.
