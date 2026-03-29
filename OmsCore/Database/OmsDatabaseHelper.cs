using Microsoft.Data.Sqlite;
using OmsCore.Models;
using System.Security.Cryptography;
using System.Text;

namespace OmsCore.Database;

/// <summary>
/// OMS 主数据库：用户、产品、订单
/// </summary>
public class OmsDatabaseHelper
{
    private readonly string _cs;

    public OmsDatabaseHelper(string dbPath = "oms.db")
    {
        _cs = $"Data Source={dbPath}";
        InitializeTables();
    }

    // ────────────────────────────────────
    //  建表
    // ────────────────────────────────────

    private void InitializeTables()
    {
        using var conn = Open();

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS Users (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Username  TEXT    NOT NULL UNIQUE,
                Password  TEXT    NOT NULL,
                Email     TEXT    NOT NULL DEFAULT '',
                CreatedAt TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """);

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS Products (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                SkuCode     TEXT    NOT NULL UNIQUE,
                Name        TEXT    NOT NULL,
                Category    TEXT    NOT NULL DEFAULT '',
                Unit        TEXT    NOT NULL DEFAULT '件',
                Price       REAL    NOT NULL DEFAULT 0,
                Description TEXT    NOT NULL DEFAULT '',
                IsActive    INTEGER NOT NULL DEFAULT 1,
                CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """);

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS Orders (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderNo   TEXT    NOT NULL UNIQUE,
                Status    INTEGER NOT NULL DEFAULT 0,
                Remark    TEXT    NOT NULL DEFAULT '',
                CreatedBy TEXT    NOT NULL DEFAULT '',
                CreatedAt TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                UpdatedAt TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """);

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS OrderItems (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId   INTEGER NOT NULL,
                SkuCode   TEXT    NOT NULL,
                Quantity  INTEGER NOT NULL DEFAULT 1,
                UnitPrice REAL    NOT NULL DEFAULT 0,
                Remark    TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
            );
        """);
    }

    // ────────────────────────────────────
    //  用户管理
    // ────────────────────────────────────

    public (bool Success, string Message) Register(string username, string password, string email = "")
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "用户名至少需要 3 个字符");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "密码至少需要 6 个字符");

        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand(
                "INSERT INTO Users (Username, Password, Email) VALUES (@u, @p, @e)", conn);
            cmd.Parameters.AddWithValue("@u", username.Trim());
            cmd.Parameters.AddWithValue("@p", HashPwd(password));
            cmd.Parameters.AddWithValue("@e", email.Trim());
            cmd.ExecuteNonQuery();
            return (true, "注册成功！");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, "该用户名已被占用");
        }
        catch (Exception ex)
        {
            return (false, $"注册失败：{ex.Message}");
        }
    }

    public (bool Success, string Message, OmsUserInfo? User) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "用户名或密码不能为空", null);

        using var conn = Open();
        using var cmd = new SqliteCommand(
            "SELECT Id, Username, Email, Password FROM Users WHERE Username=@u", conn);
        cmd.Parameters.AddWithValue("@u", username.Trim());
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return (false, "用户名不存在", null);

        if (HashPwd(password) != r.GetString(3))
            return (false, "密码错误", null);

        return (true, "登录成功！", new OmsUserInfo
        {
            Id       = r.GetInt32(0),
            Username = r.GetString(1),
            Email    = r.GetString(2),
        });
    }

    public List<OmsUserInfo> GetAllUsers()
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("SELECT Id, Username, Email FROM Users ORDER BY Id", conn);
        var list = new List<OmsUserInfo>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new OmsUserInfo { Id = r.GetInt32(0), Username = r.GetString(1), Email = r.GetString(2) });
        return list;
    }

    // ────────────────────────────────────
    //  产品管理
    // ────────────────────────────────────

    public List<Product> GetAllProducts(bool activeOnly = false)
    {
        using var conn = Open();
        var sql = activeOnly
            ? "SELECT * FROM Products WHERE IsActive=1 ORDER BY Id"
            : "SELECT * FROM Products ORDER BY Id";
        using var cmd = new SqliteCommand(sql, conn);
        var list = new List<Product>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapProduct(r));
        return list;
    }

    public Product? GetProductBySku(string skuCode)
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("SELECT * FROM Products WHERE SkuCode=@s", conn);
        cmd.Parameters.AddWithValue("@s", skuCode);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapProduct(r) : null;
    }

    public (bool Success, string Message) AddProduct(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.SkuCode)) return (false, "SKU 编码不能为空");
        if (string.IsNullOrWhiteSpace(p.Name))    return (false, "产品名称不能为空");

        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand("""
                INSERT INTO Products (SkuCode,Name,Category,Unit,Price,Description,IsActive)
                VALUES (@s,@n,@c,@u,@p,@d,@a)
            """, conn);
            BindProduct(cmd, p);
            cmd.ExecuteNonQuery();
            return (true, "产品添加成功");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, $"SKU 编码 [{p.SkuCode}] 已存在");
        }
        catch (Exception ex)
        {
            return (false, $"添加失败：{ex.Message}");
        }
    }

    public (bool Success, string Message) UpdateProduct(Product p)
    {
        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand("""
                UPDATE Products
                SET Name=@n,Category=@c,Unit=@u,Price=@p,Description=@d,IsActive=@a
                WHERE SkuCode=@s
            """, conn);
            BindProduct(cmd, p);
            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, "更新成功") : (false, "产品不存在");
        }
        catch (Exception ex) { return (false, $"更新失败：{ex.Message}"); }
    }

    public (bool Success, string Message) DeleteProduct(string skuCode)
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("DELETE FROM Products WHERE SkuCode=@s", conn);
        cmd.Parameters.AddWithValue("@s", skuCode);
        var rows = cmd.ExecuteNonQuery();
        return rows > 0 ? (true, "删除成功") : (false, "产品不存在");
    }

    // ────────────────────────────────────
    //  订单管理
    // ────────────────────────────────────

    public List<Order> GetAllOrders()
    {
        using var conn = Open();
        var orders = new Dictionary<int, Order>();

        using (var cmd = new SqliteCommand(
            "SELECT Id,OrderNo,Status,Remark,CreatedBy,CreatedAt,UpdatedAt FROM Orders ORDER BY Id DESC",
            conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                orders[r.GetInt32(0)] = new Order
                {
                    Id        = r.GetInt32(0),
                    OrderNo   = r.GetString(1),
                    Status    = (OrderStatus)r.GetInt32(2),
                    Remark    = r.GetString(3),
                    CreatedBy = r.GetString(4),
                    CreatedAt = r.GetString(5),
                    UpdatedAt = r.GetString(6),
                };

        if (orders.Count > 0)
        {
            using var cmd2 = new SqliteCommand("""
                SELECT oi.Id,oi.OrderId,oi.SkuCode,p.Name,oi.Quantity,oi.UnitPrice,oi.Remark
                FROM OrderItems oi
                LEFT JOIN Products p ON p.SkuCode=oi.SkuCode
            """, conn);
            using var r2 = cmd2.ExecuteReader();
            while (r2.Read())
            {
                var oid = r2.GetInt32(1);
                if (orders.TryGetValue(oid, out var o))
                    o.Items.Add(new OrderItem
                    {
                        Id        = r2.GetInt32(0),
                        OrderId   = oid,
                        SkuCode   = r2.GetString(2),
                        SkuName   = r2.IsDBNull(3) ? r2.GetString(2) : r2.GetString(3),
                        Quantity  = r2.GetInt32(4),
                        UnitPrice = r2.GetDecimal(5),
                        Remark    = r2.GetString(6),
                    });
            }
        }

        return [.. orders.Values];
    }

    public (bool Success, string Message, int OrderId) CreateOrder(Order order)
    {
        try
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();

            var orderNo = $"OMS-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            using var cmd = new SqliteCommand("""
                INSERT INTO Orders (OrderNo,Status,Remark,CreatedBy)
                VALUES (@no,@st,@rm,@by);
                SELECT last_insert_rowid();
            """, conn, tx);
            cmd.Parameters.AddWithValue("@no", orderNo);
            cmd.Parameters.AddWithValue("@st", (int)order.Status);
            cmd.Parameters.AddWithValue("@rm", order.Remark);
            cmd.Parameters.AddWithValue("@by", order.CreatedBy);
            var orderId = Convert.ToInt32(cmd.ExecuteScalar());

            foreach (var item in order.Items)
            {
                using var ic = new SqliteCommand("""
                    INSERT INTO OrderItems (OrderId,SkuCode,Quantity,UnitPrice,Remark)
                    VALUES (@oid,@sku,@qty,@price,@rm)
                """, conn, tx);
                ic.Parameters.AddWithValue("@oid",   orderId);
                ic.Parameters.AddWithValue("@sku",   item.SkuCode);
                ic.Parameters.AddWithValue("@qty",   item.Quantity);
                ic.Parameters.AddWithValue("@price", item.UnitPrice);
                ic.Parameters.AddWithValue("@rm",    item.Remark);
                ic.ExecuteNonQuery();
            }

            tx.Commit();
            return (true, $"订单 {orderNo} 创建成功", orderId);
        }
        catch (Exception ex)
        {
            return (false, $"创建失败：{ex.Message}", 0);
        }
    }

    public (bool Success, string Message) UpdateOrderStatus(int orderId, OrderStatus newStatus)
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("""
            UPDATE Orders SET Status=@s, UpdatedAt=datetime('now','localtime') WHERE Id=@id
        """, conn);
        cmd.Parameters.AddWithValue("@s",  (int)newStatus);
        cmd.Parameters.AddWithValue("@id", orderId);
        var rows = cmd.ExecuteNonQuery();
        return rows > 0 ? (true, "状态更新成功") : (false, "订单不存在");
    }

    public (bool Success, string Message) DeleteOrder(int orderId)
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("DELETE FROM Orders WHERE Id=@id AND Status=0", conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        var rows = cmd.ExecuteNonQuery();
        return rows > 0 ? (true, "删除成功") : (false, "只能删除草稿状态的订单");
    }

    // ────────────────────────────────────
    //  工具方法
    // ────────────────────────────────────

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_cs);
        conn.Open();
        return conn;
    }

    private static void ExecNQ(SqliteConnection conn, string sql)
    {
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static Product MapProduct(SqliteDataReader r) => new()
    {
        Id          = r.GetInt32(0),
        SkuCode     = r.GetString(1),
        Name        = r.GetString(2),
        Category    = r.GetString(3),
        Unit        = r.GetString(4),
        Price       = r.GetDecimal(5),
        Description = r.GetString(6),
        IsActive    = r.GetInt32(7) == 1,
        CreatedAt   = r.GetString(8),
    };

    private static void BindProduct(SqliteCommand cmd, Product p)
    {
        cmd.Parameters.AddWithValue("@s", p.SkuCode.Trim());
        cmd.Parameters.AddWithValue("@n", p.Name.Trim());
        cmd.Parameters.AddWithValue("@c", p.Category.Trim());
        cmd.Parameters.AddWithValue("@u", p.Unit.Trim());
        cmd.Parameters.AddWithValue("@p", p.Price);
        cmd.Parameters.AddWithValue("@d", p.Description.Trim());
        cmd.Parameters.AddWithValue("@a", p.IsActive ? 1 : 0);
    }

    public static string HashPwd(string pwd)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pwd));
        return Convert.ToHexString(bytes).ToLower();
    }
}
