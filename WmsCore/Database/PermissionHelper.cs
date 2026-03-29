using Microsoft.Data.Sqlite;

namespace WmsCore.Database;

/// <summary>
/// 权限数据库操作类：管理权限点、角色、用户角色分配
/// </summary>
public class PermissionHelper
{
    private readonly string _connectionString;

    public PermissionHelper(string dbPath = "wms.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeTables();
        SeedDefaultPermissions();
    }

    // ─────────────────────────────────────────────
    //  建表
    // ─────────────────────────────────────────────

    private void InitializeTables()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // 权限点表（可动态增减，不写死）
        ExecuteNonQuery(conn, """
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

        // 角色表
        ExecuteNonQuery(conn, """
            CREATE TABLE IF NOT EXISTS Roles (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL UNIQUE,
                Code        TEXT    NOT NULL UNIQUE,
                Description TEXT    NOT NULL DEFAULT '',
                CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """);

        // 角色-权限点 多对多
        ExecuteNonQuery(conn, """
            CREATE TABLE IF NOT EXISTS RolePermissions (
                RoleCode       TEXT NOT NULL,
                PermissionCode TEXT NOT NULL,
                PRIMARY KEY (RoleCode, PermissionCode)
            );
        """);

        // 用户-角色 多对多
        ExecuteNonQuery(conn, """
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId   INTEGER NOT NULL,
                RoleCode TEXT    NOT NULL,
                PRIMARY KEY (UserId, RoleCode)
            );
        """);
    }

    // ─────────────────────────────────────────────
    //  预置默认权限点和 admin 角色
    // ─────────────────────────────────────────────

    private void SeedDefaultPermissions()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // 已有数据则跳过（幂等）
        using var checkCmd = new SqliteCommand(
            "SELECT COUNT(*) FROM PermissionItems", conn);
        var count = (long)(checkCmd.ExecuteScalar() ?? 0L);
        if (count > 0) return;

        var defaultItems = new[]
        {
            // 菜单
            ("menu.dashboard",        "仪表盘",       "主导航", 1, "",               10, ""),
            ("menu.warehouse",        "仓库管理",      "主导航", 1, "",               20, ""),
            ("menu.inbound",          "入库管理",      "主导航", 1, "",               30, ""),
            ("menu.outbound",         "出库管理",      "主导航", 1, "",               40, ""),
            ("menu.inventory",        "库存查询",      "主导航", 1, "",               50, ""),
            ("menu.admin",            "系统管理",      "主导航", 1, "",               90, ""),

            // 子菜单
            ("menu.admin.users",      "用户管理",      "系统管理", 2, "menu.admin",    91, ""),
            ("menu.admin.roles",      "角色管理",      "系统管理", 2, "menu.admin",    92, ""),
            ("menu.admin.permissions","权限点管理",    "系统管理", 2, "menu.admin",    93, ""),

            // 入库功能按钮
            ("btn.inbound.view",      "查看入库单",    "入库管理", 3, "menu.inbound",  0, ""),
            ("btn.inbound.create",    "新增入库单",    "入库管理", 3, "menu.inbound",  0, ""),
            ("btn.inbound.edit",      "编辑入库单",    "入库管理", 3, "menu.inbound",  0, ""),
            ("btn.inbound.delete",    "删除入库单",    "入库管理", 3, "menu.inbound",  0, ""),
            ("btn.inbound.confirm",   "确认入库",      "入库管理", 3, "menu.inbound",  0, ""),

            // 出库功能按钮
            ("btn.outbound.view",     "查看出库单",    "出库管理", 3, "menu.outbound", 0, ""),
            ("btn.outbound.create",   "新增出库单",    "出库管理", 3, "menu.outbound", 0, ""),
            ("btn.outbound.edit",     "编辑出库单",    "出库管理", 3, "menu.outbound", 0, ""),
            ("btn.outbound.delete",   "删除出库单",    "出库管理", 3, "menu.outbound", 0, ""),
            ("btn.outbound.confirm",  "确认出库",      "出库管理", 3, "menu.outbound", 0, ""),

            // 仓库管理按钮
            ("btn.warehouse.view",    "查看仓库",      "仓库管理", 3, "menu.warehouse", 0, ""),
            ("btn.warehouse.create",  "新增仓库",      "仓库管理", 3, "menu.warehouse", 0, ""),
            ("btn.warehouse.edit",    "编辑仓库",      "仓库管理", 3, "menu.warehouse", 0, ""),
            ("btn.warehouse.delete",  "删除仓库",      "仓库管理", 3, "menu.warehouse", 0, ""),

            // 库存查询按钮
            ("btn.inventory.view",    "查看库存",      "库存查询", 3, "menu.inventory", 0, ""),
            ("btn.inventory.export",  "导出库存",      "库存查询", 3, "menu.inventory", 0, ""),

            // 系统管理按钮
            ("btn.admin.user.create", "新增用户",      "系统管理", 3, "menu.admin.users", 0, ""),
            ("btn.admin.user.edit",   "编辑用户",      "系统管理", 3, "menu.admin.users", 0, ""),
            ("btn.admin.user.delete", "删除用户",      "系统管理", 3, "menu.admin.users", 0, ""),
            ("btn.admin.role.assign", "分配用户角色",  "系统管理", 3, "menu.admin.users", 0, ""),
        };

        using var tx = conn.BeginTransaction();
        foreach (var (code, name, group, type, parent, sort, desc) in defaultItems)
        {
            using var cmd = new SqliteCommand("""
                INSERT OR IGNORE INTO PermissionItems
                    (Code, Name, GroupName, Type, ParentCode, SortOrder, Description)
                VALUES (@c, @n, @g, @t, @p, @s, @d)
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

        // 默认角色：超级管理员（拥有所有权限）
        using var roleCmd = new SqliteCommand("""
            INSERT OR IGNORE INTO Roles (Name, Code, Description)
            VALUES ('超级管理员', 'admin', '拥有所有权限'),
                   ('仓库操作员', 'operator', '基本仓库操作'),
                   ('只读访问',   'viewer',   '仅查看，不可修改')
        """, conn, tx);
        roleCmd.ExecuteNonQuery();

        // admin 角色默认拥有全部权限（直接从 defaultItems 提取，避免在事务内查同一连接）
        var allCodes = defaultItems.Select(x => x.Item1).ToList();

        foreach (var c in allCodes)
        {
            using var rpCmd = new SqliteCommand("""
                INSERT OR IGNORE INTO RolePermissions (RoleCode, PermissionCode)
                VALUES ('admin', @c)
            """, conn, tx);
            rpCmd.Parameters.AddWithValue("@c", c);
            rpCmd.ExecuteNonQuery();
        }

        // operator 基础权限（查看 + 入出库基本操作）
        var operatorCodes = new[]
        {
            "menu.dashboard", "menu.inbound", "menu.outbound", "menu.inventory",
            "btn.inbound.view", "btn.inbound.create", "btn.inbound.confirm",
            "btn.outbound.view", "btn.outbound.create", "btn.outbound.confirm",
            "btn.inventory.view",
        };
        foreach (var c in operatorCodes)
        {
            using var rpCmd = new SqliteCommand("""
                INSERT OR IGNORE INTO RolePermissions (RoleCode, PermissionCode)
                VALUES ('operator', @c)
            """, conn, tx);
            rpCmd.Parameters.AddWithValue("@c", c);
            rpCmd.ExecuteNonQuery();
        }

        // viewer 仅查看
        var viewerCodes = new[]
        {
            "menu.dashboard", "menu.inventory",
            "btn.inventory.view",
        };
        foreach (var c in viewerCodes)
        {
            using var rpCmd = new SqliteCommand("""
                INSERT OR IGNORE INTO RolePermissions (RoleCode, PermissionCode)
                VALUES ('viewer', @c)
            """, conn, tx);
            rpCmd.Parameters.AddWithValue("@c", c);
            rpCmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // ─────────────────────────────────────────────
    //  权限点管理
    // ─────────────────────────────────────────────

    /// <summary>获取所有权限点</summary>
    public List<PermissionItem> GetAllPermissions()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT Id,Code,Name,GroupName,Type,ParentCode,SortOrder,Description FROM PermissionItems ORDER BY SortOrder,Id",
            conn);
        var result = new List<PermissionItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            result.Add(MapPermissionItem(r));
        return result;
    }

    /// <summary>新增权限点</summary>
    public (bool Success, string Message) AddPermission(PermissionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Code))
            return (false, "权限编码不能为空");
        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "权限名称不能为空");

        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand("""
                INSERT INTO PermissionItems (Code,Name,GroupName,Type,ParentCode,SortOrder,Description)
                VALUES (@c,@n,@g,@t,@p,@s,@d)
            """, conn);
            cmd.Parameters.AddWithValue("@c", item.Code.Trim());
            cmd.Parameters.AddWithValue("@n", item.Name.Trim());
            cmd.Parameters.AddWithValue("@g", item.Group.Trim());
            cmd.Parameters.AddWithValue("@t", (int)item.Type);
            cmd.Parameters.AddWithValue("@p", item.ParentCode.Trim());
            cmd.Parameters.AddWithValue("@s", item.SortOrder);
            cmd.Parameters.AddWithValue("@d", item.Description.Trim());
            cmd.ExecuteNonQuery();
            return (true, "权限点添加成功");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, $"权限编码 [{item.Code}] 已存在");
        }
        catch (Exception ex)
        {
            return (false, $"添加失败：{ex.Message}");
        }
    }

    /// <summary>更新权限点</summary>
    public (bool Success, string Message) UpdatePermission(PermissionItem item)
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand("""
                UPDATE PermissionItems
                SET Name=@n, GroupName=@g, Type=@t, ParentCode=@p, SortOrder=@s, Description=@d
                WHERE Code=@c
            """, conn);
            cmd.Parameters.AddWithValue("@c", item.Code);
            cmd.Parameters.AddWithValue("@n", item.Name);
            cmd.Parameters.AddWithValue("@g", item.Group);
            cmd.Parameters.AddWithValue("@t", (int)item.Type);
            cmd.Parameters.AddWithValue("@p", item.ParentCode);
            cmd.Parameters.AddWithValue("@s", item.SortOrder);
            cmd.Parameters.AddWithValue("@d", item.Description);
            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, "更新成功") : (false, "权限点不存在");
        }
        catch (Exception ex)
        {
            return (false, $"更新失败：{ex.Message}");
        }
    }

    /// <summary>删除权限点（同时清理 RolePermissions 中的引用）</summary>
    public (bool Success, string Message) DeletePermission(string code)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        ExecuteNonQuery(conn, tx,
            "DELETE FROM RolePermissions WHERE PermissionCode=@c",
            ("@c", code));
        var rows = ExecuteNonQuery(conn, tx,
            "DELETE FROM PermissionItems WHERE Code=@c",
            ("@c", code));
        tx.Commit();
        return rows > 0 ? (true, "删除成功") : (false, "权限点不存在");
    }

    // ─────────────────────────────────────────────
    //  角色管理
    // ─────────────────────────────────────────────

    /// <summary>获取所有角色（含权限编码列表）</summary>
    public List<Role> GetAllRoles()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var roles = new Dictionary<string, Role>();

        using (var cmd = new SqliteCommand(
            "SELECT Id,Name,Code,Description,CreatedAt FROM Roles ORDER BY Id", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var role = new Role
                {
                    Id          = r.GetInt32(0),
                    Name        = r.GetString(1),
                    Code        = r.GetString(2),
                    Description = r.GetString(3),
                    CreatedAt   = r.GetString(4),
                };
                roles[role.Code] = role;
            }

        using (var cmd = new SqliteCommand(
            "SELECT RoleCode, PermissionCode FROM RolePermissions", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var rc = r.GetString(0);
                if (roles.TryGetValue(rc, out var role))
                    role.PermissionCodes.Add(r.GetString(1));
            }

        return [.. roles.Values];
    }

    /// <summary>新增角色</summary>
    public (bool Success, string Message) AddRole(Role role)
    {
        if (string.IsNullOrWhiteSpace(role.Code))
            return (false, "角色编码不能为空");
        if (string.IsNullOrWhiteSpace(role.Name))
            return (false, "角色名称不能为空");

        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand("""
                INSERT INTO Roles (Name, Code, Description)
                VALUES (@n, @c, @d)
            """, conn);
            cmd.Parameters.AddWithValue("@n", role.Name.Trim());
            cmd.Parameters.AddWithValue("@c", role.Code.Trim());
            cmd.Parameters.AddWithValue("@d", role.Description.Trim());
            cmd.ExecuteNonQuery();
            return (true, "角色创建成功");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, $"角色编码 [{role.Code}] 已存在");
        }
        catch (Exception ex)
        {
            return (false, $"创建失败：{ex.Message}");
        }
    }

    /// <summary>更新角色基本信息</summary>
    public (bool Success, string Message) UpdateRole(Role role)
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand(
                "UPDATE Roles SET Name=@n, Description=@d WHERE Code=@c", conn);
            cmd.Parameters.AddWithValue("@n", role.Name);
            cmd.Parameters.AddWithValue("@d", role.Description);
            cmd.Parameters.AddWithValue("@c", role.Code);
            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, "更新成功") : (false, "角色不存在");
        }
        catch (Exception ex)
        {
            return (false, $"更新失败：{ex.Message}");
        }
    }

    /// <summary>删除角色（同时清理关联数据）</summary>
    public (bool Success, string Message) DeleteRole(string roleCode)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        ExecuteNonQuery(conn, tx, "DELETE FROM RolePermissions WHERE RoleCode=@c", ("@c", roleCode));
        ExecuteNonQuery(conn, tx, "DELETE FROM UserRoles WHERE RoleCode=@c", ("@c", roleCode));
        var rows = ExecuteNonQuery(conn, tx, "DELETE FROM Roles WHERE Code=@c", ("@c", roleCode));
        tx.Commit();
        return rows > 0 ? (true, "角色已删除") : (false, "角色不存在");
    }

    /// <summary>为角色批量设置权限（全量覆盖）</summary>
    public (bool Success, string Message) SetRolePermissions(string roleCode, IEnumerable<string> permissionCodes)
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            // 先清除旧的
            ExecuteNonQuery(conn, tx,
                "DELETE FROM RolePermissions WHERE RoleCode=@c",
                ("@c", roleCode));

            // 再批量插入新的
            foreach (var pc in permissionCodes)
            {
                using var cmd = new SqliteCommand("""
                    INSERT OR IGNORE INTO RolePermissions (RoleCode, PermissionCode)
                    VALUES (@r, @p)
                """, conn, tx);
                cmd.Parameters.AddWithValue("@r", roleCode);
                cmd.Parameters.AddWithValue("@p", pc);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return (true, "权限设置成功");
        }
        catch (Exception ex)
        {
            return (false, $"设置失败：{ex.Message}");
        }
    }

    // ─────────────────────────────────────────────
    //  用户角色分配
    // ─────────────────────────────────────────────

    /// <summary>获取用户当前拥有的角色编码列表</summary>
    public List<string> GetUserRoleCodes(int userId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT RoleCode FROM UserRoles WHERE UserId=@id", conn);
        cmd.Parameters.AddWithValue("@id", userId);
        var result = new List<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) result.Add(r.GetString(0));
        return result;
    }

    /// <summary>为用户批量设置角色（全量覆盖）</summary>
    public (bool Success, string Message) SetUserRoles(int userId, IEnumerable<string> roleCodes)
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ExecuteNonQuery(conn, tx,
                "DELETE FROM UserRoles WHERE UserId=@id",
                ("@id", (object)userId));

            foreach (var rc in roleCodes)
            {
                using var cmd = new SqliteCommand("""
                    INSERT OR IGNORE INTO UserRoles (UserId, RoleCode)
                    VALUES (@id, @rc)
                """, conn, tx);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.Parameters.AddWithValue("@rc", rc);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return (true, "角色分配成功");
        }
        catch (Exception ex)
        {
            return (false, $"分配失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户合并后的所有权限编码（多角色权限取并集）
    /// </summary>
    public HashSet<string> GetUserPermissions(int userId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("""
            SELECT DISTINCT rp.PermissionCode
            FROM UserRoles ur
            JOIN RolePermissions rp ON ur.RoleCode = rp.RoleCode
            WHERE ur.UserId = @id
        """, conn);
        cmd.Parameters.AddWithValue("@id", userId);

        var result = new HashSet<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) result.Add(r.GetString(0));
        return result;
    }

    /// <summary>检查用户是否拥有某个权限点</summary>
    public bool HasPermission(int userId, string permissionCode)
        => GetUserPermissions(userId).Contains(permissionCode);

    /// <summary>获取所有用户及其角色信息</summary>
    public List<UserRoleInfo> GetAllUserRoles()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var users = new Dictionary<int, UserRoleInfo>();

        using (var cmd = new SqliteCommand(
            "SELECT Id, Username FROM Users ORDER BY Id", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                users[r.GetInt32(0)] = new UserRoleInfo
                {
                    UserId   = r.GetInt32(0),
                    Username = r.GetString(1),
                };

        using (var cmd = new SqliteCommand(
            "SELECT UserId, RoleCode FROM UserRoles", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var uid = r.GetInt32(0);
                if (users.TryGetValue(uid, out var u))
                    u.RoleCodes.Add(r.GetString(1));
            }

        return [.. users.Values];
    }

    // ─────────────────────────────────────────────
    //  内部工具方法
    // ─────────────────────────────────────────────

    private static PermissionItem MapPermissionItem(SqliteDataReader r) => new()
    {
        Id          = r.GetInt32(0),
        Code        = r.GetString(1),
        Name        = r.GetString(2),
        Group       = r.GetString(3),
        Type        = (PermissionType)r.GetInt32(4),
        ParentCode  = r.GetString(5),
        SortOrder   = r.GetInt32(6),
        Description = r.GetString(7),
    };

    private static void ExecuteNonQuery(SqliteConnection conn, string sql,
        params (string Name, object Value)[] ps)
    {
        using var cmd = new SqliteCommand(sql, conn);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        cmd.ExecuteNonQuery();
    }

    private static int ExecuteNonQuery(SqliteConnection conn, SqliteTransaction tx,
        string sql, params (string Name, object Value)[] ps)
    {
        using var cmd = new SqliteCommand(sql, conn, tx);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return cmd.ExecuteNonQuery();
    }
}
