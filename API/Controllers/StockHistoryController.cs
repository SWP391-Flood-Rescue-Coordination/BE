using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

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
    /// Lấy danh sách lịch sử kho theo thứ tự mới nhất đến cũ nhất.
    /// Hỗ trợ chuẩn hóa search: ?searchBy=fromTo&keyword=abc
    /// </summary>
    /// <param name="type">Loại giao dịch: IN hoặc OUT (tuỳ chọn)</param>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> GetStockHistory([FromQuery] string? type, [FromQuery] string? searchBy = null, [FromQuery] string? keyword = null)
    {
        if (type != null)
        {
            var upper = type.ToUpper();
            if (upper != "IN" && upper != "OUT")
                return BadRequest(new { Success = false, Message = "type phải là 'IN' hoặc 'OUT'." });

            type = upper;
        }

        var query = _context.StockHistories.AsQueryable();

        // Chuẩn hóa search backend: Mỗi trang chỉ tìm theo 1 field đúng mục đích nghiệp vụ
        if (!string.IsNullOrWhiteSpace(searchBy))
        {
            // Whitelist các trường được phép search cho endpoint này: CHỈ tìm theo ID (Mã phiếu)
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
                if (searchBy == "id")
                {
                    query = query.Where(s => s.Id.ToString() == keyword);
                }
            }
        }

        if (!string.IsNullOrEmpty(type))
            query = query.Where(s => s.Type == type);

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
    /// Tạo phiếu nhập kho vật tư cứu trợ khi tiếp nhận hàng từ các nguồn hỗ trợ.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ImportStock([FromBody] ImportStockRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { Success = false, Message = "Danh sách vật tư không được rỗng." });
        }

        var sourceUnitName = await ResolveActiveStockUnitNameAsync(request.Source, forImport: true);
        if (sourceUnitName == null)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Nguồn nhập không tồn tại hoặc đã ngưng sử dụng. Vui lòng chọn từ danh sách đơn vị hợp lệ."
            });
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var importedItemsBodyList = new List<string>();

                foreach (var itemInput in request.Items)
                {
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null)
                    {
                        throw new Exception($"Vật tư với ID {itemInput.ItemId} không tồn tại trong hệ thống.");
                    }

                    if (!reliefItem.IsActive)
                    {
                        throw new Exception($"Vật tư '{reliefItem.ItemName}' (ID: {itemInput.ItemId}) đang trong trạng thái ngưng hoạt động.");
                    }

                    if (itemInput.Quantity <= 0)
                    {
                        throw new Exception($"Số lượng vật tư '{reliefItem.ItemName}' phải lớn hơn 0.");
                    }

                    reliefItem.Quantity += itemInput.Quantity;
                    importedItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                var bodyContent = string.Join(",", importedItemsBodyList);

                var history = new StockHistory
                {
                    Type = "IN",
                    Date = DateTime.UtcNow,
                    FromTo = sourceUnitName,
                    Body = bodyContent.Length > 500 ? bodyContent.Substring(0, 497) + "..." : bodyContent,
                    Note = request.Note?.Length > 500 ? request.Note.Substring(0, 497) + "..." : request.Note
                };

                _context.StockHistories.Add(history);
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
            return BadRequest(new { Success = false, Message = "Lỗi khi tạo phiếu nhập kho: " + ex.Message });
        }
    }

    /// <summary>
    /// Tạo phiếu xuất kho vật tư cứu trợ để điều phối đến đơn vị nhận (tỉnh/xóm).
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ExportStock([FromBody] ExportStockRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { Success = false, Message = "Danh sách vật tư xuất không được rỗng." });

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var manager = await _context.Users.FindAsync(userId);
                if (manager == null)
                    return Unauthorized(new { Success = false, Message = "Không tìm thấy thông tin Manager." });

                // 1. Địa điểm nhận (Destination)
                if (string.IsNullOrWhiteSpace(request.Destination))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Đơn vị nhận hàng là bắt buộc. Vui lòng chọn từ danh sách đơn vị xuất."
                    });
                }

                var destinationUnitName = await ResolveActiveStockUnitNameAsync(request.Destination, forImport: false);
                if (destinationUnitName == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Đơn vị xuất không tồn tại hoặc đã ngưng sử dụng. Vui lòng chọn từ danh sách đơn vị hợp lệ."
                    });
                }

                string finalDestination = destinationUnitName;

                // 2. Kiểm tra tồn kho và chuẩn bị Body
                var exportItemsBodyList = new List<string>();
                int totalExportQuantity = 0;

                foreach (var itemInput in request.Items)
                {
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null)
                        throw new Exception($"Vật tư với ID {itemInput.ItemId} không tồn tại.");

                    if (reliefItem.Quantity < itemInput.Quantity)
                        throw new Exception($"Vật tư '{reliefItem.ItemName}' không đủ tồn kho (Cần: {itemInput.Quantity}, Hiện có: {reliefItem.Quantity}).");

                    reliefItem.Quantity -= itemInput.Quantity;
                    totalExportQuantity += itemInput.Quantity;
                    exportItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                // 3. Tạo lịch sử
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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Xuất kho thành công.",
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
            return BadRequest(new { Success = false, Message = "Lỗi khi xuất kho: " + ex.Message });
        }
    }

    /// <summary>
    /// Helper resolve đơn vị hợp lệ từ bảng stock_units.
    /// - forImport = true  -> chỉ nhận đơn vị supports_import + is_active
    /// - forImport = false -> chỉ nhận đơn vị supports_export + is_active
    /// Cho phép FE gửi theo UnitCode hoặc UnitName.
    /// </summary>
    private async Task<string?> ResolveActiveStockUnitNameAsync(string rawValue, bool forImport)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var normalized = rawValue.Trim();
        var normalizedUpper = normalized.ToUpperInvariant();

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
