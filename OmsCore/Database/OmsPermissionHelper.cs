using Microsoft.Data.Sqlite;

namespace OmsCore.Database;

/// <summary>
/// OMS 权限管理：权限点、角色、用户角色分配（与 WMS 独立部署，各自一套）
/// </summary>
public class OmsPermissionHelper
{
    private readonly string _cs;

    public OmsPermissionHelper(string dbPath = "oms.db")
    {
        _cs = $"Data Source={dbPath}";
        InitializeTables();
        SeedDefaults();
    }

    // ────────────────────────────────────
    //  建表
    // ────────────────────────────────────

    private void InitializeTables()
    {
        using var conn = Open();

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS PermissionItems (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Code        TEXT    NOT NULL UNIQUE,
                Name        TEXT    NOT NULL,
                GroupName   TEXT    NOT NULL DEFAULT '',
                Type        INTEGER NOT NULL DEFAULT 1,
                ParentCode  TEXT    NOT NULL DEFAULT '',
                SortOrder   INTEGER NOT NULL DEFAULT 0,
                Description TEXT    NOT NULL DEFAULT ''
            );
        """);

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS Roles (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL UNIQUE,
                Code        TEXT    NOT NULL UNIQUE,
                Description TEXT    NOT NULL DEFAULT '',
                CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """);

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS RolePermissions (
                RoleCode       TEXT NOT NULL,
                PermissionCode TEXT NOT NULL,
                PRIMARY KEY (RoleCode, PermissionCode)
            );
        """);

        ExecNQ(conn, """
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId   INTEGER NOT NULL,
                RoleCode TEXT    NOT NULL,
                PRIMARY KEY (UserId, RoleCode)
            );
        """);
    }

    // ────────────────────────────────────
    //  预置默认权限点
    // ────────────────────────────────────

    private void SeedDefaults()
    {
        using var conn = Open();
        using var check = new SqliteCommand("SELECT COUNT(*) FROM PermissionItems", conn);
        if ((long)(check.ExecuteScalar() ?? 0L) > 0) return;

        var items = new[]
        {
            // OMS 主菜单
            ("menu.dashboard",          "仪表盘",       "主导航",   1, "",                        10, ""),
            ("menu.products",           "产品管理",      "主导航",   1, "",                        20, ""),
            ("menu.orders",             "订单管理",      "主导航",   1, "",                        30, ""),
            ("menu.inventory",          "库存查询",      "主导航",   1, "",                        40, ""),
            ("menu.admin",              "系统管理",      "主导航",   1, "",                        90, ""),

            // 系统管理子菜单
            ("menu.admin.users",        "用户管理",      "系统管理", 2, "menu.admin",              91, ""),
            ("menu.admin.roles",        "角色管理",      "系统管理", 2, "menu.admin",              92, ""),
            ("menu.admin.permissions",  "权限点管理",    "系统管理", 2, "menu.admin",              93, ""),

            // 产品功能按钮
            ("btn.products.view",       "查看产品",      "产品管理", 3, "menu.products",           0, ""),
            ("btn.products.create",     "新增产品",      "产品管理", 3, "menu.products",           0, ""),
            ("btn.products.edit",       "编辑产品",      "产品管理", 3, "menu.products",           0, ""),
            ("btn.products.delete",     "删除产品",      "产品管理", 3, "menu.products",           0, ""),

            // 订单功能按钮
            ("btn.orders.view",         "查看订单",      "订单管理", 3, "menu.orders",             0, ""),
            ("btn.orders.create",       "新增订单",      "订单管理", 3, "menu.orders",             0, ""),
            ("btn.orders.submit",       "提交订单",      "订单管理", 3, "menu.orders",             0, ""),
            ("btn.orders.cancel",       "取消订单",      "订单管理", 3, "menu.orders",             0, ""),
            ("btn.orders.delete",       "删除草稿订单",  "订单管理", 3, "menu.orders",             0, ""),

            // 库存查询
            ("btn.inventory.view",      "查看库存",      "库存查询", 3, "menu.inventory",          0, ""),

            // 系统管理按钮
            ("btn.admin.user.create",   "新增用户",      "系统管理", 3, "menu.admin.users",        0, ""),
            ("btn.admin.user.edit",     "编辑用户",      "系统管理", 3, "menu.admin.users",        0, ""),
            ("btn.admin.user.delete",   "删除用户",      "系统管理", 3, "menu.admin.users",        0, ""),
            ("btn.admin.role.assign",   "分配用户角色",  "系统管理", 3, "menu.admin.users",        0, ""),
        };

        using var tx = conn.BeginTransaction();

        foreach (var (code, name, group, type, parent, sort, desc) in items)
        {
            using var cmd = new SqliteCommand("""
                INSERT OR IGNORE INTO PermissionItems
                    (Code,Name,GroupName,Type,ParentCode,SortOrder,Description)
                VALUES (@c,@n,@g,@t,@p,@s,@d)
            """, conn, tx);
            cmd.Parameters.AddWithValue("@c", code);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@g", group);
            cmd.Parameters.AddWithValue("@t", type);
            cmd.Parameters.AddWithValue("@p", parent);
            cmd.Parameters.AddWithValue("@s", sort);
            cmd.Parameters.AddWithValue("@d", desc);
            cmd.ExecuteNonQuery();
        }

        using var roles = new SqliteCommand("""
            INSERT OR IGNORE INTO Roles (Name,Code,Description) VALUES
            ('超级管理员', 'admin',    '拥有所有权限'),
            ('订单操作员', 'operator', '可管理订单和查询库存'),
            ('只读访问',   'viewer',   '仅查看，不可修改')
        """, conn, tx);
        roles.ExecuteNonQuery();

        // admin 全部权限
        foreach (var (code, _, _, _, _, _, _) in items)
        {
            using var rp = new SqliteCommand("""
                INSERT OR IGNORE INTO RolePermissions (RoleCode,PermissionCode) VALUES ('admin',@c)
            """, conn, tx);
            rp.Parameters.AddWithValue("@c", code);
            rp.ExecuteNonQuery();
        }

        // operator 权限
        var opCodes = new[]
        {
            "menu.dashboard","menu.products","menu.orders","menu.inventory",
            "btn.products.view",
            "btn.orders.view","btn.orders.create","btn.orders.submit","btn.orders.cancel",
            "btn.inventory.view",
        };
        foreach (var c in opCodes)
        {
            using var rp = new SqliteCommand("""
                INSERT OR IGNORE INTO RolePermissions (RoleCode,PermissionCode) VALUES ('operator',@c)
            """, conn, tx);
            rp.Parameters.AddWithValue("@c", c);
            rp.ExecuteNonQuery();
        }

        // viewer 权限
        foreach (var c in new[] { "menu.dashboard", "menu.inventory", "btn.inventory.view" })
        {
            using var rp = new SqliteCommand("""
                INSERT OR IGNORE INTO RolePermissions (RoleCode,PermissionCode) VALUES ('viewer',@c)
            """, conn, tx);
            rp.Parameters.AddWithValue("@c", c);
            rp.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // ────────────────────────────────────
    //  权限点 CRUD
    // ────────────────────────────────────

    public List<OmsPermissionItem> GetAllPermissions()
    {
        using var conn = Open();
        using var cmd = new SqliteCommand(
            "SELECT Id,Code,Name,GroupName,Type,ParentCode,SortOrder,Description FROM PermissionItems ORDER BY SortOrder,Id", conn);
        var list = new List<OmsPermissionItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapPerm(r));
        return list;
    }

    public (bool, string) AddPermission(OmsPermissionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Code)) return (false, "编码不能为空");
        if (string.IsNullOrWhiteSpace(item.Name)) return (false, "名称不能为空");
        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand("""
                INSERT INTO PermissionItems (Code,Name,GroupName,Type,ParentCode,SortOrder,Description)
                VALUES (@c,@n,@g,@t,@p,@s,@d)
            """, conn);
            BindPerm(cmd, item);
            cmd.ExecuteNonQuery();
            return (true, "添加成功");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { return (false, $"编码 [{item.Code}] 已存在"); }
        catch (Exception ex) { return (false, $"失败：{ex.Message}"); }
    }

    public (bool, string) UpdatePermission(OmsPermissionItem item)
    {
        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand("""
                UPDATE PermissionItems SET Name=@n,GroupName=@g,Type=@t,ParentCode=@p,SortOrder=@s,Description=@d WHERE Code=@c
            """, conn);
            BindPerm(cmd, item);
            return cmd.ExecuteNonQuery() > 0 ? (true, "更新成功") : (false, "权限点不存在");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public (bool, string) DeletePermission(string code)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        ExecNQ(conn, tx, "DELETE FROM RolePermissions WHERE PermissionCode=@c", ("@c", code));
        var rows = ExecNQ(conn, tx, "DELETE FROM PermissionItems WHERE Code=@c", ("@c", code));
        tx.Commit();
        return rows > 0 ? (true, "删除成功") : (false, "不存在");
    }

    // ────────────────────────────────────
    //  角色 CRUD
    // ────────────────────────────────────

    public List<OmsRole> GetAllRoles()
    {
        using var conn = Open();
        var roles = new Dictionary<string, OmsRole>();

        using (var cmd = new SqliteCommand("SELECT Id,Name,Code,Description,CreatedAt FROM Roles ORDER BY Id", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                roles[r.GetString(2)] = new OmsRole
                {
                    Id = r.GetInt32(0), Name = r.GetString(1), Code = r.GetString(2),
                    Description = r.GetString(3), CreatedAt = r.GetString(4),
                };

        using (var cmd = new SqliteCommand("SELECT RoleCode,PermissionCode FROM RolePermissions", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                if (roles.TryGetValue(r.GetString(0), out var role))
                    role.PermissionCodes.Add(r.GetString(1));

        return [.. roles.Values];
    }

    public (bool, string) AddRole(OmsRole role)
    {
        if (string.IsNullOrWhiteSpace(role.Code)) return (false, "角色编码不能为空");
        if (string.IsNullOrWhiteSpace(role.Name)) return (false, "角色名称不能为空");
        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand("INSERT INTO Roles (Name,Code,Description) VALUES (@n,@c,@d)", conn);
            cmd.Parameters.AddWithValue("@n", role.Name.Trim());
            cmd.Parameters.AddWithValue("@c", role.Code.Trim());
            cmd.Parameters.AddWithValue("@d", role.Description.Trim());
            cmd.ExecuteNonQuery();
            return (true, "角色创建成功");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { return (false, $"编码 [{role.Code}] 已存在"); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public (bool, string) UpdateRole(OmsRole role)
    {
        try
        {
            using var conn = Open();
            using var cmd = new SqliteCommand("UPDATE Roles SET Name=@n,Description=@d WHERE Code=@c", conn);
            cmd.Parameters.AddWithValue("@n", role.Name); cmd.Parameters.AddWithValue("@d", role.Description); cmd.Parameters.AddWithValue("@c", role.Code);
            return cmd.ExecuteNonQuery() > 0 ? (true, "更新成功") : (false, "角色不存在");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public (bool, string) DeleteRole(string roleCode)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        ExecNQ(conn, tx, "DELETE FROM RolePermissions WHERE RoleCode=@c",  ("@c", roleCode));
        ExecNQ(conn, tx, "DELETE FROM UserRoles       WHERE RoleCode=@c",  ("@c", roleCode));
        var rows = ExecNQ(conn, tx, "DELETE FROM Roles WHERE Code=@c",     ("@c", roleCode));
        tx.Commit();
        return rows > 0 ? (true, "删除成功") : (false, "角色不存在");
    }

    public (bool, string) SetRolePermissions(string roleCode, IEnumerable<string> codes)
    {
        try
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            ExecNQ(conn, tx, "DELETE FROM RolePermissions WHERE RoleCode=@c", ("@c", roleCode));
            foreach (var pc in codes)
            {
                using var cmd = new SqliteCommand(
                    "INSERT OR IGNORE INTO RolePermissions (RoleCode,PermissionCode) VALUES (@r,@p)", conn, tx);
                cmd.Parameters.AddWithValue("@r", roleCode);
                cmd.Parameters.AddWithValue("@p", pc);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
            return (true, "权限设置成功");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    // ────────────────────────────────────
    //  用户角色分配
    // ────────────────────────────────────

    public List<string> GetUserRoleCodes(int userId)
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("SELECT RoleCode FROM UserRoles WHERE UserId=@id", conn);
        cmd.Parameters.AddWithValue("@id", userId);
        var list = new List<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    public (bool, string) SetUserRoles(int userId, IEnumerable<string> roleCodes)
    {
        try
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            ExecNQ(conn, tx, "DELETE FROM UserRoles WHERE UserId=@id", ("@id", (object)userId));
            foreach (var rc in roleCodes)
            {
                using var cmd = new SqliteCommand(
                    "INSERT OR IGNORE INTO UserRoles (UserId,RoleCode) VALUES (@id,@rc)", conn, tx);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.Parameters.AddWithValue("@rc", rc);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
            return (true, "角色分配成功");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public HashSet<string> GetUserPermissions(int userId)
    {
        using var conn = Open();
        using var cmd = new SqliteCommand("""
            SELECT DISTINCT rp.PermissionCode
            FROM UserRoles ur
            JOIN RolePermissions rp ON ur.RoleCode=rp.RoleCode
            WHERE ur.UserId=@id
        """, conn);
        cmd.Parameters.AddWithValue("@id", userId);
        var set = new HashSet<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) set.Add(r.GetString(0));
        return set;
    }

    public bool HasPermission(int userId, string code) => GetUserPermissions(userId).Contains(code);

    public List<OmsUserRoleInfo> GetAllUserRoles()
    {
        using var conn = Open();
        var users = new Dictionary<int, OmsUserRoleInfo>();

        using (var cmd = new SqliteCommand("SELECT Id,Username FROM Users ORDER BY Id", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                users[r.GetInt32(0)] = new OmsUserRoleInfo { UserId = r.GetInt32(0), Username = r.GetString(1) };

        using (var cmd = new SqliteCommand("SELECT UserId,RoleCode FROM UserRoles", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                if (users.TryGetValue(r.GetInt32(0), out var u))
                    u.RoleCodes.Add(r.GetString(1));

        return [.. users.Values];
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

    private static int ExecNQ(SqliteConnection conn, SqliteTransaction tx, string sql,
        params (string Name, object Value)[] ps)
    {
        using var cmd = new SqliteCommand(sql, conn, tx);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return cmd.ExecuteNonQuery();
    }

    private static OmsPermissionItem MapPerm(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0), Code = r.GetString(1), Name = r.GetString(2),
        Group = r.GetString(3), Type = (OmsPermissionType)r.GetInt32(4),
        ParentCode = r.GetString(5), SortOrder = r.GetInt32(6), Description = r.GetString(7),
    };

    private static void BindPerm(SqliteCommand cmd, OmsPermissionItem item)
    {
        cmd.Parameters.AddWithValue("@c", item.Code.Trim());
        cmd.Parameters.AddWithValue("@n", item.Name.Trim());
        cmd.Parameters.AddWithValue("@g", item.Group.Trim());
        cmd.Parameters.AddWithValue("@t", (int)item.Type);
        cmd.Parameters.AddWithValue("@p", item.ParentCode.Trim());
        cmd.Parameters.AddWithValue("@s", item.SortOrder);
        cmd.Parameters.AddWithValue("@d", item.Description.Trim());
    }
}
