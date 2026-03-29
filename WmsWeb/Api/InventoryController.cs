using Microsoft.AspNetCore.Mvc;
using WmsCore.Database;

namespace WmsWeb.Api;

/// <summary>
/// WMS 对外 API：供 OMS 等外部系统查询库存数据
/// 路由前缀：/api/inventory
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly DatabaseHelper _db;

    public InventoryController(DatabaseHelper db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/inventory?sku=xxx
    /// 查询库存。目前返回模拟数据；后续接入真实库存表后替换此方法内容即可。
    /// </summary>
    [HttpGet]
    public IActionResult GetInventory([FromQuery] string? sku = null)
    {
        // TODO: 后续接入真实库存表后，替换此处查询逻辑
        var allItems = new List<InventoryItem>
        {
            new("SKU-001", "商品A", 120, "WH-001", "A-01-01"),
            new("SKU-002", "商品B",  45, "WH-001", "A-01-02"),
            new("SKU-003", "商品C",   0, "WH-001", "B-02-01"),
            new("SKU-004", "商品D",  88, "WH-002", "C-01-01"),
            new("SKU-005", "商品E",  33, "WH-002", "C-02-03"),
        };

        var result = string.IsNullOrWhiteSpace(sku)
            ? allItems
            : allItems.Where(x => x.SkuCode.Contains(sku, StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(new ApiResult<List<InventoryItem>>(true, "查询成功", result));
    }

    /// <summary>
    /// GET /api/health
    /// OMS 用于检测 WMS 是否在线
    /// </summary>
    [HttpGet("/api/health")]
    public IActionResult Health() => Ok(new { status = "ok", system = "WMS", time = DateTime.Now });
}

// ──────────────────────────────────────
//  响应数据模型
// ──────────────────────────────────────

public record InventoryItem(
    string SkuCode,
    string SkuName,
    int    Quantity,
    string WarehouseNo,
    string Location)
{
    public string UpdatedAt { get; init; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

public record ApiResult<T>(bool Success, string Message, T? Data);
