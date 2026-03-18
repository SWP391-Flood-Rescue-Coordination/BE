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

## Luồng hoạt động tiêu chuẩn khi trực tiếp nhận hàng cứu trợ
1. **Manager** đăng nhập vào API và nhận token với quyền MANAGER.
2. Khi tiếp nhận lô hàng, **Manager** truy cập Swagger, vào mục `StockHistory` và mở `POST /api/StockHistory/import`.
3. Điền các mục thông tin tổng quan (`source`, `note`) và cung cấp một mảng JSON chứa các `items` (gồm `itemId` và `quantity`). Nhấn Execute (Try it out).
4. Phản hồi thành công (Status 200). Các `ReliefItems` khai báo sẽ được hệ thống tăng số tồn kho.
5. (Tuỳ chọn) Manager có thể gọi API `GET /api/StockHistory?type=IN` để xem lại phiếu thông tin nhập kho cùng nguồn tiếp nhận (body, source, note) hoặc `GET /api/ReliefItem/low-stock` để theo dõi các vật tư.
