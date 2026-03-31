using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// StockHistoryController: Quản lý lịch sử biến động kho và các nghiệp vụ Nhập/Xuất vật tư cứu trợ.
/// Đảm bảo tính nhất quán dữ liệu tồn kho thông qua các giao dịch (Transactions).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StockHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor khởi tạo StockHistoryController với DbContext.
    /// </summary>
    public StockHistoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// ADMIN / MANAGER - Lấy danh sách lịch sử biến động kho.
    /// Hỗ trợ lọc theo loại giao dịch (Nhập/Xuất) và tìm kiếm theo Mã phiếu.
    /// </summary>
    /// <param name="type">Loại giao dịch: 'IN' (Nhập) hoặc 'OUT' (Xuất).</param>
    /// <param name="searchBy">Trường tìm kiếm (Hiện tại chỉ hỗ trợ 'id').</param>
    /// <param name="keyword">Từ khóa tìm kiếm (Mã ID của phiếu).</param>
    /// <returns>Danh sách lịch sử kho được sắp xếp mới nhất lên đầu.</returns>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> GetStockHistory([FromQuery] string? type, [FromQuery] string? searchBy = null, [FromQuery] string? keyword = null)
    {
        // 1. Chuẩn hóa tham số 'type'
        if (type != null)
        {
            var upper = type.ToUpper();
            if (upper != "IN" && upper != "OUT")
                return BadRequest(new { Success = false, Message = "Tham số 'type' không hợp lệ. Phải là 'IN' hoặc 'OUT'." });

            type = upper;
        }

        var query = _context.StockHistories.AsQueryable();

        // 2. Xử lý logic tìm kiếm (Search)
        if (!string.IsNullOrWhiteSpace(searchBy))
        {
            // Kiểm tra Whitelist các trường được cho phép tìm kiếm
            var allowedFields = new[] { "id" };
            if (!allowedFields.Contains(searchBy))
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Trường tìm kiếm '{searchBy}' không hợp lệ. Chỉ chấp nhận trường: {string.Join(", ", allowedFields)}" 
                });
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                // Tìm kiếm theo ID (Mã phiếu nhập/xuất)
                if (searchBy == "id")
                {
                    query = query.Where(s => s.Id.ToString() == keyword);
                }
            }
        }

        // 3. Lọc theo loại (Nhập/Xuất)
        if (!string.IsNullOrEmpty(type))
            query = query.Where(s => s.Type == type);

        // 4. Thực thi truy vấn, sắp xếp theo ngày giảm dần và Map sang DTO
        var items = await query
            .OrderByDescending(s => s.Date)
            .Select(s => new StockHistoryDto
            {
                Id     = s.Id,
                Type   = s.Type,
                Date   = s.Date,
                Body   = s.Body,
                FromTo = s.FromTo,
                Note   = s.Note
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Type    = type ?? "ALL",
            Count   = items.Count,
            Data    = items
        });
    }

    /// <summary>
    /// MANAGER - Thực hiện Nhập kho vật tư cứu trợ (Ví dụ: Tiếp nhận đồ quyên góp, hàng tiếp tế).
    /// Quy trình gồm: Cập nhật tăng số lượng tồn kho và ghi nhận lịch sử phiếu nhập (TYPE = IN).
    /// </summary>
    /// <param name="request">Thông tin phiếu nhập: Nguồn nhập (Source), Danh sách vật tư (Items), Ghi chú.</param>
    /// <returns>Thông tin phiếu nhập vừa tạo.</returns>
    [HttpPost("import")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ImportStock([FromBody] ImportStockRequest request)
    {
        // 1. Kiểm tra danh sách vật tư không được để trống
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { Success = false, Message = "Danh sách vật tư không được rỗng." });
        }

        // 2. Kiểm tra tính hợp lệ của Nguồn nhập hàng (Source Unit)
        var sourceUnitName = await ResolveActiveStockUnitNameAsync(request.Source, forImport: true);
        if (sourceUnitName == null)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Nguồn nhập không tồn tại hoặc đã bị vô hiệu hóa. Vui lòng chọn nguồn hợp lệ."
            });
        }

        // Sử dụng ExecutionStrategy để đảm bảo transaction hoạt động ổn định trong môi trường Cloud/Retriable logic
        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                // Bắt đầu Transaction để bảo toàn tính toàn vẹn dữ liệu
                using var transaction = await _context.Database.BeginTransactionAsync();
                var importedItemsBodyList = new List<string>();

                // Lặp qua từng vật tư để cập nhật
                foreach (var itemInput in request.Items)
                {
                    // Tìm vật tư cứu trợ trong DB
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null)
                    {
                        throw new Exception($"Vật tư với ID {itemInput.ItemId} không tồn tại.");
                    }

                    // Kiểm tra trạng thái hoạt động của vật tư
                    if (!reliefItem.IsActive)
                    {
                        throw new Exception($"Vật tư '{reliefItem.ItemName}' hiện đang ở trạng thái ngưng hoạt động.");
                    }

                    // Kiểm tra số lượng hợp lệ
                    if (itemInput.Quantity <= 0)
                    {
                        throw new Exception($"Số lượng nhập cho '{reliefItem.ItemName}' phải lớn hơn 0.");
                    }

                    // Tăng số lượng tồn kho
                    reliefItem.Quantity += itemInput.Quantity;
                    
                    // Lưu lại thông tin ID-Số lượng để ghi vào Body của phiếu
                    importedItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                // Chuyển danh sách ID-Số lượng thành chuỗi CSV
                var bodyContent = string.Join(",", importedItemsBodyList);

                // Tạo thực thể Lịch sử kho (Phiếu nhập)
                var history = new StockHistory
                {
                    Type = "IN",
                    Date = DateTime.UtcNow,
                    FromTo = sourceUnitName,
                    // Cắt chuỗi nếu vượt quá giới hạn độ dài DB (500 ký tự)
                    Body = bodyContent.Length > 500 ? bodyContent.Substring(0, 497) + "..." : bodyContent,
                    Note = request.Note?.Length > 500 ? request.Note.Substring(0, 497) + "..." : request.Note
                };

                _context.StockHistories.Add(history);
                
                // Lưu tất cả thay đổi và xác nhận Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Tạo phiếu nhập kho thành công.",
                    HistoryId = history.Id,
                    Data = new
                    {
                        history.Type,
                        history.Date,
                        history.FromTo,
                        history.Body,
                        history.Note
                    }
                });
            });
        }
        catch (Exception ex)
        {
            // Rollback tự động khi có Exception trong quá trình SaveChangesAsync
            return BadRequest(new { Success = false, Message = "Lỗi khi tạo phiếu nhập kho: " + ex.Message });
        }
    }

    /// <summary>
    /// MANAGER - Thực hiện Xuất kho vật tư cứu trợ (Điều phối hàng cứu trợ đến vùng lũ/đơn vị tuyến dưới).
    /// Quy trình: Kiểm tra tồn kho -> Giảm tồn kho (nhiều bản ghi) -> Tạo phiếu xuất (TYPE = OUT).
    /// Sử dụng Transaction để đảm bảo không bị trừ kho sai nếu có lỗi xảy ra giữa chừng.
    /// </summary>
    /// <param name="request">Thông tin phiếu xuất: Đơn vị nhận (Destination), Danh sách vật tư (Items), Ghi chú.</param>
    [HttpPost("export")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ExportStock([FromBody] ExportStockRequest request)
    {
        // 1. Kiểm tra đầu vào
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { Success = false, Message = "Danh sách vật tư xuất không được rỗng." });

        // 2. Trích xuất người thực hiện từ Token
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Phiên làm việc hết hạn hoặc không hợp lệ." });

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var manager = await _context.Users.FindAsync(userId);
                if (manager == null)
                    return Unauthorized(new { Success = false, Message = "Người dùng không có dữ liệu quản lý trong hệ thống." });

                // 3. Kiểm tra tính hợp lệ của Đơn vị nhận hàng (Destination Unit)
                if (string.IsNullOrWhiteSpace(request.Destination))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Bắt buộc phải chọn đơn vị nhận hàng."
                    });
                }

                var destinationUnitName = await ResolveActiveStockUnitNameAsync(request.Destination, forImport: false);
                if (destinationUnitName == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Đơn vị nhận hàng không tồn tại hoặc đã bị vô hiệu hóa."
                    });
                }

                string finalDestination = destinationUnitName;

                // 4. Duyệt qua danh sách hàng cần xuất và kiểm tra Tồn kho (Inventory Check)
                var exportItemsBodyList = new List<string>();

                foreach (var itemInput in request.Items)
                {
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null)
                        throw new Exception($"Vật tư (ID: {itemInput.ItemId}) không tồn tại.");

                    // Ràng buộc nghiệp vụ: Không cho phép xuất vượt quá số lượng trong kho
                    if (reliefItem.Quantity < itemInput.Quantity)
                        throw new Exception($"'{reliefItem.ItemName}' không đủ tồn kho (Cần xuất: {itemInput.Quantity}, Tồn kho: {reliefItem.Quantity}).");

                    // Trừ tồn kho
                    reliefItem.Quantity -= itemInput.Quantity;
                    exportItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                // 5. Khởi tạo ghi nhận Lịch sử phiếu xuất (Phiếu xuất kho)
                var bodyContent = string.Join(",", exportItemsBodyList);
                var finalNote = $"Địa điểm nhận: {finalDestination}";
                if (!string.IsNullOrEmpty(request.Note))
                    finalNote += $" | Ghi chú: {request.Note}";

                var history = new StockHistory
                {
                    Type = "OUT",
                    Date = DateTime.UtcNow,
                    FromTo = finalDestination,
                    Body = bodyContent.Length > 500 ? bodyContent.Substring(0, 497) + "..." : bodyContent,
                    Note = finalNote.Length > 500 ? finalNote.Substring(0, 497) + "..." : finalNote
                };

                _context.StockHistories.Add(history);

                // 6. Lưu và chốt Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Tạo phiếu xuất kho thành công.",
                    HistoryId = history.Id,
                    Data = new
                    {
                        history.Type,
                        history.FromTo,
                        history.Body,
                        history.Note
                    }
                });
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = "Lỗi khi xử lý xuất kho: " + ex.Message });
        }
    }

    /// <summary>
    /// Phương thức hỗ trợ: Truy vấn tên Đơn vị Kho hợp lệ từ bảng StockUnits (dựa trên Mã hoặc Tên).
    /// </summary>
    /// <param name="rawValue">Mã đơn vị (UnitCode) hoặc Tên đơn vị (UnitName) từ FE gửi lên.</param>
    /// <param name="forImport">True nếu kiểm tra cho nghiệp vụ Nhập, False cho nghiệp vụ Xuất.</param>
    /// <returns>Tên đơn vị chuẩn nếu tìm thấy và đang hoạt động, ngược lại trả về null.</returns>
    private async Task<string?> ResolveActiveStockUnitNameAsync(string rawValue, bool forImport)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var normalized = rawValue.Trim();
        var normalizedUpper = normalized.ToUpperInvariant();

        // Xây dựng câu truy vấn dựa trên vai trò đơn vị (Nhập/Xuất) và trạng thái Active
        var query = _context.StockUnits
            .AsNoTracking()
            .Where(u => u.IsActive && (forImport ? u.SupportsImport : u.SupportsExport));

        var resolvedName = await query
            .Where(u => u.UnitCode.ToUpper() == normalizedUpper || u.UnitName.ToUpper() == normalizedUpper)
            .Select(u => u.UnitName)
            .FirstOrDefaultAsync();

        return resolvedName;
    }
}
