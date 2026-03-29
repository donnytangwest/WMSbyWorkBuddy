namespace OmsCore.Models;

// ──────────────────────────────────────
//  产品 SKU
// ──────────────────────────────────────

public class Product
{
    public int     Id          { get; set; }
    public string  SkuCode     { get; set; } = "";   // 唯一 SKU 编码
    public string  Name        { get; set; } = "";
    public string  Category    { get; set; } = "";
    public string  Unit        { get; set; } = "件";
    public decimal Price       { get; set; } = 0;
    public string  Description { get; set; } = "";
    public bool    IsActive    { get; set; } = true;
    public string  CreatedAt   { get; set; } = "";
}

// ──────────────────────────────────────
//  订单
// ──────────────────────────────────────

public enum OrderStatus
{
    Draft      = 0,  // 草稿
    Submitted  = 1,  // 已提交（等待 WMS 处理）
    Processing = 2,  // WMS 处理中
    Completed  = 3,  // 已完成
    Cancelled  = 4,  // 已取消
}

public class Order
{
    public int          Id          { get; set; }
    public string       OrderNo     { get; set; } = "";   // 订单号 OMS-yyyyMMdd-xxxx
    public OrderStatus  Status      { get; set; } = OrderStatus.Draft;
    public string       Remark      { get; set; } = "";
    public string       CreatedBy   { get; set; } = "";
    public string       CreatedAt   { get; set; } = "";
    public string       UpdatedAt   { get; set; } = "";

    // 订单行（查询时填充）
    public List<OrderItem> Items { get; set; } = new();

    public string StatusText => Status switch
    {
        OrderStatus.Draft      => "草稿",
        OrderStatus.Submitted  => "已提交",
        OrderStatus.Processing => "处理中",
        OrderStatus.Completed  => "已完成",
        OrderStatus.Cancelled  => "已取消",
        _                      => "未知",
    };

    public string StatusBadge => Status switch
    {
        OrderStatus.Draft      => "secondary",
        OrderStatus.Submitted  => "primary",
        OrderStatus.Processing => "warning",
        OrderStatus.Completed  => "success",
        OrderStatus.Cancelled  => "danger",
        _                      => "light",
    };
}

public class OrderItem
{
    public int     Id        { get; set; }
    public int     OrderId   { get; set; }
    public string  SkuCode   { get; set; } = "";
    public string  SkuName   { get; set; } = "";   // 冗余，查询时填充
    public int     Quantity  { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
    public string  Remark    { get; set; } = "";
}
