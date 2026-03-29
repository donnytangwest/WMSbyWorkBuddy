using System.Net.Http.Json;

namespace OmsCore.Api;

// ──────────────────────────────────────
//  WMS 返回的库存信息
// ──────────────────────────────────────
public class WmsInventoryItem
{
    public string SkuCode     { get; set; } = "";
    public string SkuName     { get; set; } = "";
    public int    Quantity    { get; set; } = 0;
    public string WarehouseNo { get; set; } = "";
    public string Location    { get; set; } = "";
    public string UpdatedAt   { get; set; } = "";
}

public class WmsApiResult<T>
{
    public bool   Success { get; set; } = false;
    public string Message { get; set; } = "";
    public T?     Data    { get; set; }
}

/// <summary>
/// OMS 调用 WMS 的 HTTP API 客户端
/// OMS 与 WMS 可部署在不同城市，通过接口传输数据
/// </summary>
public class WmsApiClient
{
    private readonly HttpClient _http;
    private readonly string     _baseUrl;

    public WmsApiClient(string wmsBaseUrl = "http://localhost:5188")
    {
        _baseUrl = wmsBaseUrl.TrimEnd('/');
        _http    = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl + "/"),
            Timeout     = TimeSpan.FromSeconds(15),
        };
    }

    /// <summary>
    /// 查询库存（支持按 SKU 过滤）
    /// </summary>
    public async Task<(bool Success, string Message, List<WmsInventoryItem> Items)> GetInventoryAsync(string? skuCode = null)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(skuCode)
                ? "api/inventory"
                : $"api/inventory?sku={Uri.EscapeDataString(skuCode)}";

            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<WmsApiResult<List<WmsInventoryItem>>>();
            if (result == null) return (false, "返回数据为空", new());
            return (result.Success, result.Message, result.Data ?? new());
        }
        catch (HttpRequestException ex)
        {
            return (false, $"无法连接到 WMS（{_baseUrl}）：{ex.Message}", new());
        }
        catch (TaskCanceledException)
        {
            return (false, "请求超时，请检查 WMS 是否在线", new());
        }
        catch (Exception ex)
        {
            return (false, $"请求失败：{ex.Message}", new());
        }
    }

    /// <summary>
    /// 检查 WMS 是否在线
    /// </summary>
    public async Task<bool> PingAsync()
    {
        try
        {
            var resp = await _http.GetAsync("api/health");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
