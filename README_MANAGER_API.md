# Hướng dẫn sử dụng API cho Manager

## Tổng quan
Manager có trách nhiệm quản lý hệ thống phân phối và lưu kho, bao gồm việc theo dõi tồn kho và nhập hàng hóa từ các nguồn khác nhau.

## API Endpoints mới: Quản lý Kho (Stock Management)

### 1. Nhập kho vật tư cứu trợ
- **Endpoint**: `POST /api/ReliefItem/import`
- **Quyền hạn**: `Roles = "MANAGER"`
- **Mô tả**: Cho phép Manager tạo phiếu nhập kho vật tư cứu trợ khi tiếp nhận hàng từ các nguồn hỗ trợ (ví dụ như nhà tài trợ, tổ chức thiện nguyện, cơ quan khác,…). Hệ thống sẽ tự động cập nhật số lượng tồn kho của các mặt hàng và lưu lại lịch sử thay đổi để truy vết nguồn gốc.
- **Body (JSON)**:
    ```json
    {
      "source": "Hội Chữ thập đỏ tỉnh",
      "location": "Kho trung tâm thành phố (Khu B)",
      "items": [
        {
          "itemId": 1,
          "quantity": 500
        },
        {
          "itemId": 2,
          "quantity": 1000
        }
      ]
    }
    ```
- **Thông số chi tiết**:
  - `source` (string): Bắt buộc. Nhập tên của nguồn hỗ trợ vật tư hoặc nhà tài trợ.
  - `location` (string): Bắt buộc. Địa chỉ tiếp nhận, lưu hàng hóa nhập kho.
  - `items` (array): Danh sách các vật tư nhận được. Bắt buộc có ít nhất 1 mặt hàng.
    - `itemId` (int): Bắt buộc. Mã của vật tư tồn tại trong hệ thống.
    - `quantity` (int): Bắt buộc. Số lượng vật tư (phải > 0).

- **Kiểm tra nghiệp vụ (Validation)**:
  - Kháng lỗi (400) nếu `items` rỗng, hoặc có bất kỳ số lượng `quantity` nào ≤ 0.
  - Sẽ trả lỗi nếu bất kỳ `itemId` nào không tồn tại trong danh mục hệ thống.
  - Giao dịch (Transaction) sẽ bị hủy bỏ (rollback) an toàn nếu có bất kỳ lỗi nào xảy ra trong quá trình cập nhật kho.

### 2. Xem lịch sử nhập/xuất kho
- **Endpoint**: `GET /api/StockHistory`
- **Quyền hạn**: `Roles = "MANAGER, ADMIN"`
- **Tham số (Query Params)**:
  - `type` (string, optional): Lọc theo loại (chỉ nhận "IN" hoặc "OUT"). Truyền `type=IN` nếu muốn xem danh sách các đợt nhập kho vật tư.
- **Mô tả**: Trả về danh sách lịch sử lưu kho theo trình tự từ mới nhất đến cũ nhất. Lịch sử nhập lô hàng cũng phản ánh rõ khối lượng, loại mặt hàng, nhà tài trợ đã tạo từ lệnh POST trên.

## Luồng hoạt động tiêu chuẩn khi trực tiếp nhận hàng cứu trợ
1. **Manager** đăng nhập vào API và nhận token với quyền MANAGER.
2. Khi tiếp nhận lô hàng, **Manager** truy cập Swagger, vào mục `ReliefItem` và mở `POST /api/ReliefItem/import`.
3. Điền các mục thông tin tổng quan (`source`, `location`) và cung cấp một mảng JSON chứa các `itemId`, kèm số lượng nhập tương ứng. Nhấn Execute (Try it out).
4. Phản hồi thành công (Status 200). Các `ReliefItems` khai báo sẽ được hệ thống tăng số tồn kho.
5. (Tuỳ chọn) Manager có thể gọi API `GET /api/StockHistory?type=IN` để xem lại phiếu thông tin nhập kho cùng nguồn tiếp nhận (body, source, note) hoặc `GET /api/ReliefItem/low-stock` để theo dõi các vật tư.
