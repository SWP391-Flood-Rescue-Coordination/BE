using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER")]
public class ManagerController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ManagerController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách tất cả phương tiện với khả năng lọc theo trạng thái (Chỉ Manager)
    /// </summary>
    /// <param name="status">Trạng thái lọc (Available, InUse, Maintenance, etc.)</param>
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetAllVehicles([FromQuery] string? status = null)
    {
        var query = _context.Vehicles
            .Include(v => v.VehicleType)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(v => v.Status == status);
        }

        var vehicles = await query
            .OrderBy(v => v.VehicleCode)
            .Select(v => new VehicleResponseDto
            {
                VehicleId = v.VehicleId,
                VehicleCode = v.VehicleCode,
                VehicleName = v.VehicleName,
                VehicleTypeName = v.VehicleType != null ? v.VehicleType.TypeName : "",
                LicensePlate = v.LicensePlate,
                Capacity = v.Capacity,
                Status = v.Status,
                CurrentLocation = v.CurrentLocation,
                LastMaintenance = v.LastMaintenance,
                UpdatedAt = v.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Data = vehicles, Count = vehicles.Count });
    }

    /// <summary>
    /// Cập nhật nhanh trạng thái của phương tiện (Chỉ Manager)
    /// </summary>
    [HttpPatch("vehicles/{id}/status")]
    public async Task<IActionResult> UpdateVehicleStatus(int id, [FromBody] UpdateVehicleStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { Success = false, Message = "Trạng thái không được để trống" });
        }

        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        vehicle.Status = dto.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// Tạo phiếu xuất kho cứu trợ (Chỉ Manager)
    /// </summary>
    [HttpPost("relief-export")]
    public async Task<IActionResult> CreateReliefExport([FromBody] CreateReliefExportDto dto)
    {
        // 1. Lấy Manager ID từ Token (Sử dụng Sub claim đã gán trong JwtService)
        var userIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { Success = false, Message = "Không tìm thấy thông tin đăng nhập" });
        int managerId = int.Parse(userIdStr);

        // 2. Kiểm tra phạm vi quản lý
        bool hasScope = await _context.ManagerScopes.AnyAsync(s => s.UserId == managerId && s.RegionId == dto.DestinationRegionId);
        if (!hasScope)
        {
            return StatusCode(403, new { Success = false, Message = "Bạn không có quyền xuất kho cho vùng/đơn vị nhận này." });
        }

        // 3. Kiểm tra trạng thái phương tiện
        var vehicles = await _context.Vehicles.Where(v => dto.VehicleIds.Contains(v.VehicleId)).ToListAsync();
        if (vehicles.Count != dto.VehicleIds.Count) return BadRequest(new { Success = false, Message = "Một số phương tiện không tồn tại." });
        if (vehicles.Any(v => v.Status != "Available"))
        {
            return BadRequest(new { Success = false, Message = "Tất cả phương tiện được chọn phải ở trạng thái AVAILABLE." });
        }

        // 4. Kiểm tra tồn kho và Sức chứa
        int totalItemsCount = 0;
        foreach (var itemReq in dto.Items)
        {
            var stock = await _context.Inventories.FirstOrDefaultAsync(i => i.WarehouseId == dto.WarehouseId && i.ItemId == itemReq.ItemId);
            if (stock == null || stock.Quantity < itemReq.Quantity)
            {
                var itemInfo = await _context.ReliefItems.FindAsync(itemReq.ItemId);
                return BadRequest(new { Success = false, Message = $"Vật tư '{itemInfo?.ItemName ?? itemReq.ItemId.ToString()}' không đủ tồn kho (Cần: {itemReq.Quantity})." });
            }
            totalItemsCount += itemReq.Quantity;
        }

        int totalCapacity = vehicles.Sum(v => v.Capacity ?? 0);
        if (totalItemsCount > totalCapacity)
        {
            return BadRequest(new { Success = false, Message = $"Tổng số lượng vật tư ({totalItemsCount}) vượt quá tổng sức chứa của phương tiện ({totalCapacity})." });
        }

        // 5. Thực hiện Lưu dữ liệu (Transaction)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // A. Tạo phiếu xuất
            var exportOrder = new ReliefExportOrder
            {
                ManagerId = managerId,
                WarehouseId = dto.WarehouseId,
                DestinationRegionId = dto.DestinationRegionId,
                ExportDate = DateTime.UtcNow,
                Status = "IN_TRANSIT",
                Notes = dto.Notes
            };
            _context.ReliefExportOrders.Add(exportOrder);
            await _context.SaveChangesAsync();

            // B. Lưu chi tiết vật tư & Trừ tồn kho
            var stockBodyList = new List<string>();
            foreach (var itemReq in dto.Items)
            {
                var exportItem = new ReliefExportItem
                {
                    ExportId = exportOrder.ExportId,
                    ItemId = itemReq.ItemId,
                    Quantity = itemReq.Quantity
                };
                _context.ReliefExportItems.Add(exportItem);

                // Trừ tồn kho
                var stock = await _context.Inventories.FirstAsync(i => i.WarehouseId == dto.WarehouseId && i.ItemId == itemReq.ItemId);
                stock.Quantity -= itemReq.Quantity;

                stockBodyList.Add($"{itemReq.ItemId}-{itemReq.Quantity}");
            }

            // C. Gán phương tiện & Cập nhật trạng thái
            foreach (var vehicle in vehicles)
            {
                var exportVehicle = new ReliefExportVehicle
                {
                    ExportId = exportOrder.ExportId,
                    VehicleId = vehicle.VehicleId
                };
                _context.ReliefExportVehicles.Add(exportVehicle);

                vehicle.Status = "InUse";
                vehicle.UpdatedAt = DateTime.UtcNow;
            }

            // D. Ghi Log vào stock_history (Theo định dạng cũ của project)
            var destinationRegion = await _context.Regions.FindAsync(dto.DestinationRegionId);
            var stockLog = new StockHistory
            {
                Type = "OUT",
                Date = DateTime.UtcNow,
                Body = string.Join(",", stockBodyList),
                FromTo = $"Region: {destinationRegion?.RegionName ?? dto.DestinationRegionId.ToString()}",
                Note = $"Xuất kho cứu trợ - Phiếu #{exportOrder.ExportId}. {dto.Notes}"
            };
            _context.StockHistories.Add(stockLog);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new 
            { 
                Success = true, 
                Data = new { ExportId = exportOrder.ExportId, Status = exportOrder.Status },
                Message = "Tạo phiếu xuất kho và điều phối phương tiện thành công." 
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Success = false, Message = "Lỗi trong quá trình xử lý: " + ex.Message });
        }
    }
}
