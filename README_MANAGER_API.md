# Hướng dẫn sử dụng API cho Manager

## Tổng quan
Manager có trách nhiệm quản lý hệ thống phân phối và lưu kho, bao gồm việc theo dõi tồn kho và nhập hàng hóa từ các nguồn khác nhau.

## API Endpoints mới: Quản lý Kho (Stock Management)

### 1. Nhập kho vật tư cứu trợ
- **Endpoint**: `POST /api/StockHistory/import`
- **Quyền hạn**: `Roles = "MANAGER"`
- **Mô tả**: Cho phép Manager tạo phiếu nhập kho vật tư cứu trợ khi tiếp nhận hàng từ các nguồn hỗ trợ (ví dụ như nhà tài trợ, tổ chức thiện nguyện, cơ quan khác,…). Hệ thống sẽ tự động cập nhật số lượng tồn kho của các mặt hàng và lưu lại lịch sử thay đổi để truy vết nguồn gốc.
- **Body (JSON)**:
    ```json
    {
      "source": "Hội Chữ thập đỏ tỉnh",
      "note": "Nhập đợt lũ đầu mùa 2026",
      "items": [
        {
          "itemId": 1,
          "quantity": 500
        }
      ]
    }
    ```
- **Thông số chi tiết**:
  - `source` (string): Bắt buộc. Nhập tên của nguồn hỗ trợ vật tư hoặc nhà tài trợ.
  - `note` (string, optional): Ghi chú bổ sung cho phiếu nhập.
  - `items` (array): Danh sách các vật tư nhận được. Bắt buộc có ít nhất 1 mặt hàng.
    - `itemId` (int): Bắt buộc. Mã của vật tư tồn tại trong hệ thống.
    - `quantity` (int): Bắt buộc. Số lượng vật tư (phải > 0).

- **Kiểm tra nghiệp vụ (Validation) & Logic**:
  - Kháng lỗi (400) nếu `items` rỗng, hoặc có bất kỳ số lượng `quantity` nào ≤ 0.
  - Sẽ trả lỗi nếu bất kỳ `itemId` nào không tồn tại hoặc ở trạng thái **IsActive = false**.
  - **Lưu lịch sử**: 
    - Cột `Body`: Lưu dưới định dạng `itemId-quantity` (ví dụ: `1-500,2-1000`) để đồng nhất với dữ liệu mẫu.
    - Cột `Note`: Lưu lại ghi chú của Manager (nếu có).
- **Ghi chú**: Sử dụng Giao dịch (Transaction) để đảm bảo tính toàn vẹn dữ liệu. Nếu có lỗi, tất cả thay đổi sẽ bị hủy.

### 2. Xuất kho vật tư cứu trợ
- **Endpoint**: `POST /api/StockHistory/export`
- **Quyền hạn**: `Roles = "MANAGER"`
- **Mô tả**: Tạo phiếu xuất kho để điều phối hàng hóa đến đơn vị nhận. Hệ thống sẽ kiểm tra tồn kho trước khi thực hiện.
- **Body (JSON)**:
    ```json
    {
      "destination": "UBND Phường 22, Quận Bình Thạnh, TP.HCM",
      "note": "Xuất hỗ trợ khẩn cấp đợt 2",
      "items": [
        {
          "itemId": 1,
          "quantity": 50
        }
      ]
    }
    ```
- **Lưu ý**: 
  - `destination`: (Tùy chọn) Địa điểm hoặc đơn vị nhận hàng cụ thể. Nếu để trống, hệ thống sẽ ghi nhận là "Chưa xác định".
- **Logic xử lý & Kiểm tra**:
  1. **Kiểm tra tồn kho**: Phải đủ số lượng hàng trong kho để xuất.
  2. **Cập nhật dữ liệu**: 
     - Giảm tồn kho (`Quantity`) của vật tư.
     - Lưu lịch sử `StockHistory` loại `OUT`.

### 3. Xem lịch sử nhập/xuất kho
- **Endpoint**: `GET /api/StockHistory`
- **Quyền hạn**: `Roles = "MANAGER, ADMIN"`
- **Tham số (Query Params)**:
  - `type` (string, optional): Lọc theo loại (chỉ nhận "IN" hoặc "OUT"). Truyền `type=IN` nếu muốn xem danh sách các đợt nhập kho vật tư.
- **Mô tả**: Trả về danh sách lịch sử lưu kho theo trình tự từ mới nhất đến cũ nhất. Lịch sử nhập lô hàng cũng phản ánh rõ khối lượng, loại mặt hàng, nhà tài trợ đã tạo từ lệnh POST trên.


## API Endpoints: Quản lý Phương tiện (Vehicle Management)

### 1. Thêm mới phương tiện
- **Endpoint**: `POST /api/Vehicle`
- **Quyền hạn**: `Roles = "MANAGER, ADMIN"`
- **Mô tả**: Cho phép Manager thêm mới một phương tiện vào hệ thống. Hệ thống sẽ kiểm tra mã phương tiện (`vehicleCode`) để đảm bảo không bị trùng lặp.
- **Body (JSON)**:
    ```json
    {
      "vehicleCode": "CANO-005",
      "vehicleName": "Cano Cứu Hộ Phường 5",
      "vehicleTypeId": 1,
      "licensePlate": "29-C1 55555",
      "capacity": 12,
      "status": "AVAILABLE",
      "currentLocation": "Bến tàu Phường 5",
      "latitude": 10.762622,
      "longitude": 106.660172,
      "lastMaintenance": "2026-01-20T08:00:00Z"
    }
    ```
- **Lưu ý**:
  - `vehicleCode`: Phải là duy nhất.
  - `status`: Mặc định là `AVAILABLE`. Có thể là `AVAILABLE`, `INUSE`, `MAINTENANCE`.
  - `vehicleTypeId`: ID loại phương tiện (1: Cano/Thuyền, 2: Xe lội nước, v.v.).

### 2. Cập nhật thông tin phương tiện
- **Endpoint**: `PUT /api/Vehicle/{id}`
- **Quyền hạn**: `Roles = "MANAGER, ADMIN"`
- **Mô tả**: Cập nhật thông tin chi tiết của phương tiện. Chỉ cập nhật các trường được gửi trong body.
- **Body (JSON)**: Tương tự như thêm mới (tất cả các trường đều là tùy chọn).
- **Trường hợp sử dụng phổ biến**: 
  - Cập nhật khi xe đi bảo trì (`status: MAINTENANCE`).
  - Cập nhật vị trí tọa độ khi xe di chuyển.

### 3. Xóa phương tiện
- **Endpoint**: `DELETE /api/Vehicle/{id}`
- **Quyền hạn**: `Roles = "MANAGER, ADMIN"`
- **Mô tả**: Xóa hoàn toàn phương tiện khỏi hệ thống.
- **Ràng buộc quan trọng**: Hệ thống **không** cho phép xóa các phương tiện đã từng tham gia vào bất kỳ nhiệm vụ cứu hộ nào (`RescueOperation`). Trong trường hợp này, hãy chuyển trạng thái phương tiện sang `DISABLED` (nếu có hỗ trợ) hoặc `MAINTENANCE` để ngừng sử dụng.

### 4. Xem danh sách phương tiện
- **Endpoint**: `GET /api/Vehicle`
- **Quyền hạn**: `Roles = "MANAGER, ADMIN, COORDINATOR"`
- **Tham số (Query Params)**:
  - `status` (string, optional): Lọc theo trạng thái (ví dụ: `?status=Available`).
- **Mô tả**: Trả về danh sách phương tiện sắp xếp theo thời gian cập nhật mới nhất.

## Luồng hoạt động tiêu chuẩn (Standard Workflow)

### A. Quy trình tiếp nhận hàng cứu trợ (Stock Management)
1. **Manager** đăng nhập vào API và nhận token với quyền `MANAGER`.
2. Khi tiếp nhận lô hàng, **Manager** truy cập Swagger, vào mục `StockHistory` và mở `POST /api/StockHistory/import`.
3. Điền các mục thông tin tổng quan (`source`, `note`) và cung cấp một mảng JSON chứa các `items` (gồm `itemId` và `quantity`).
4. Nhấn Execute. Phản hồi thành công (Status 200) nghĩa là các `ReliefItems` đã được hệ thống tăng số tồn kho tương ứng.
5. (Tuỳ chọn) Manager có thể gọi API `GET /api/StockHistory?type=IN` để xem lại phiếu nhập hoặc `GET /api/ReliefItem/low-stock` để theo dõi các vật tư sắp hết.

### B. Quy trình quản lý đội tàu/xe (Vehicle Management)
1. Khi có phương tiện mới, **Manager** gọi `POST /api/Vehicle` để khai báo vào hệ thống với trạng thái `AVAILABLE`.
2. Hàng tuần/tháng, **Manager** kiểm tra danh sách phương tiện qua `GET /api/Vehicle`.
3. Nếu có phương tiện cần bảo dưỡng, **Manager** gọi `PUT /api/Vehicle/{id}` để cập nhật trạng thái sang `MAINTENANCE` và ghi nhận ngày bảo trì gần nhất.
4. Sau khi bảo trì xong, cập nhật lại trạng thái thành `AVAILABLE` để **Coordinator** có thể nhìn thấy và điều phối xe đi cứu hộ.
