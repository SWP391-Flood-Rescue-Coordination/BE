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

    public StockHistoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách lịch sử kho theo thứ tự mới nhất đến cũ nhất.
    /// Nếu truyền type = "IN" hoặc "OUT" thì chỉ lấy các dòng tương ứng.
    /// </summary>
    /// <param name="type">Loại giao dịch: IN hoặc OUT (tuỳ chọn)</param>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> GetStockHistory([FromQuery] string? type)
    {
        if (type != null)
        {
            var upper = type.ToUpper();
            if (upper != "IN" && upper != "OUT")
                return BadRequest(new { Success = false, Message = "type phải là 'IN' hoặc 'OUT'." });

            type = upper;
        }

        var query = _context.StockHistories.AsQueryable();

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
}
