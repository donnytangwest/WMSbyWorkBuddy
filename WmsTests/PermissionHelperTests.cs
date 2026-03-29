using WmsCore.Database;

namespace WmsTests;

/// <summary>
/// PermissionHelper 单元测试
/// </summary>
public class PermissionHelperTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DatabaseHelper _db;
    private readonly PermissionHelper _perm;

    public PermissionHelperTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"wms_perm_{Guid.NewGuid():N}.db");
        _db   = new DatabaseHelper(_dbPath);
        _perm = new PermissionHelper(_dbPath);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    // ─── 权限点管理 ───────────────────────────────

    [Fact]
    public void GetAllPermissions_ShouldReturnDefaultSeededItems()
    {
        var items = _perm.GetAllPermissions();
        Assert.NotEmpty(items);
        Assert.Contains(items, p => p.Code == "menu.dashboard");
        Assert.Contains(items, p => p.Code == "btn.inbound.create");
    }

    [Fact]
    public void AddPermission_ShouldSucceed()
    {
        var item = new PermissionItem
        {
            Code = "btn.test.custom",
            Name = "自定义测试权限",
            Group = "测试分组",
            Type = PermissionType.Button,
        };
        var (success, msg) = _perm.AddPermission(item);
        Assert.True(success, msg);

        var all = _perm.GetAllPermissions();
        Assert.Contains(all, p => p.Code == "btn.test.custom");
    }

    [Fact]
    public void AddPermission_DuplicateCode_ShouldFail()
    {
        var (ok1, _) = _perm.AddPermission(new PermissionItem { Code = "btn.dup", Name = "A" });
        var (ok2, msg2) = _perm.AddPermission(new PermissionItem { Code = "btn.dup", Name = "B" });
        Assert.True(ok1);
        Assert.False(ok2);
        Assert.Contains("已存在", msg2);
    }

    [Fact]
    public void UpdatePermission_ShouldChangeName()
    {
        var item = new PermissionItem { Code = "btn.upd.test", Name = "原始名称", Group = "G" };
        _perm.AddPermission(item);
        item.Name = "修改后名称";
        var (ok, _) = _perm.UpdatePermission(item);
        Assert.True(ok);

        var all = _perm.GetAllPermissions();
        Assert.Equal("修改后名称", all.First(p => p.Code == "btn.upd.test").Name);
    }

    [Fact]
    public void DeletePermission_ShouldRemoveItem()
    {
        _perm.AddPermission(new PermissionItem { Code = "btn.del.test", Name = "待删除" });
        var (ok, _) = _perm.DeletePermission("btn.del.test");
        Assert.True(ok);

        var all = _perm.GetAllPermissions();
        Assert.DoesNotContain(all, p => p.Code == "btn.del.test");
    }

    // ─── 角色管理 ──────────────────────────────────

    [Fact]
    public void GetAllRoles_ShouldReturnDefaultRoles()
    {
        var roles = _perm.GetAllRoles();
        Assert.Contains(roles, r => r.Code == "admin");
        Assert.Contains(roles, r => r.Code == "operator");
        Assert.Contains(roles, r => r.Code == "viewer");
    }

    [Fact]
    public void AddRole_ShouldSucceed()
    {
        var (ok, msg) = _perm.AddRole(new Role { Code = "supervisor", Name = "主管" });
        Assert.True(ok, msg);

        var roles = _perm.GetAllRoles();
        Assert.Contains(roles, r => r.Code == "supervisor");
    }

    [Fact]
    public void AddRole_DuplicateCode_ShouldFail()
    {
        _perm.AddRole(new Role { Code = "dup_role", Name = "A" });
        var (ok, msg) = _perm.AddRole(new Role { Code = "dup_role", Name = "B" });
        Assert.False(ok);
        Assert.Contains("已存在", msg);
    }

    [Fact]
    public void SetRolePermissions_ShouldOverwrite()
    {
        _perm.AddRole(new Role { Code = "testrole", Name = "测试角色" });
        _perm.SetRolePermissions("testrole", ["menu.dashboard", "btn.inbound.view"]);

        var roles = _perm.GetAllRoles();
        var role  = roles.First(r => r.Code == "testrole");
        Assert.Contains("menu.dashboard", role.PermissionCodes);
        Assert.Contains("btn.inbound.view", role.PermissionCodes);
        Assert.Equal(2, role.PermissionCodes.Count);

        // 覆盖写入
        _perm.SetRolePermissions("testrole", ["menu.inventory"]);
        roles = _perm.GetAllRoles();
        role  = roles.First(r => r.Code == "testrole");
        Assert.Single(role.PermissionCodes);
        Assert.Contains("menu.inventory", role.PermissionCodes);
    }

    [Fact]
    public void AdminRole_ShouldHaveAllPermissions()
    {
        var allPerms = _perm.GetAllPermissions();
        var adminRole = _perm.GetAllRoles().First(r => r.Code == "admin");
        foreach (var p in allPerms)
            Assert.Contains(p.Code, adminRole.PermissionCodes);
    }

    // ─── 用户角色 + 权限合并 ─────────────────────────

    [Fact]
    public void SetUserRoles_ShouldAssignRoles()
    {
        _db.Register("testuser1", "pass123");
        var (_, _, user) = _db.Login("testuser1", "pass123");
        Assert.NotNull(user);

        _perm.SetUserRoles(user!.Id, ["operator"]);
        var codes = _perm.GetUserRoleCodes(user.Id);
        Assert.Contains("operator", codes);
    }

    [Fact]
    public void GetUserPermissions_MultiRole_ShouldReturnUnion()
    {
        // 创建两个权限不同的角色
        _perm.AddRole(new Role { Code = "roleA", Name = "A" });
        _perm.AddRole(new Role { Code = "roleB", Name = "B" });
        _perm.SetRolePermissions("roleA", ["btn.inbound.view", "menu.inbound"]);
        _perm.SetRolePermissions("roleB", ["btn.outbound.view", "menu.outbound"]);

        _db.Register("multiuser", "pass123");
        var (_, _, user) = _db.Login("multiuser", "pass123");
        Assert.NotNull(user);

        // 分配两个角色
        _perm.SetUserRoles(user!.Id, ["roleA", "roleB"]);

        var perms = _perm.GetUserPermissions(user.Id);
        Assert.Contains("btn.inbound.view",  perms);
        Assert.Contains("menu.inbound",       perms);
        Assert.Contains("btn.outbound.view",  perms);
        Assert.Contains("menu.outbound",      perms);
    }

    [Fact]
    public void HasPermission_ShouldReturnCorrectResult()
    {
        _perm.AddRole(new Role { Code = "limited", Name = "受限角色" });
        _perm.SetRolePermissions("limited", ["menu.dashboard"]);

        _db.Register("limiteduser", "pass123");
        var (_, _, user) = _db.Login("limiteduser", "pass123");
        Assert.NotNull(user);
        _perm.SetUserRoles(user!.Id, ["limited"]);

        Assert.True(_perm.HasPermission(user.Id, "menu.dashboard"));
        Assert.False(_perm.HasPermission(user.Id, "menu.admin"));
        Assert.False(_perm.HasPermission(user.Id, "btn.inbound.create"));
    }

    [Fact]
    public void DeleteRole_ShouldCleanUpUserRoles()
    {
        _perm.AddRole(new Role { Code = "todelete", Name = "待删角色" });
        _perm.SetRolePermissions("todelete", ["menu.dashboard"]);

        _db.Register("user_del_role", "pass123");
        var (_, _, user) = _db.Login("user_del_role", "pass123");
        _perm.SetUserRoles(user!.Id, ["todelete"]);

        // 删角色后用户应无权限
        _perm.DeleteRole("todelete");
        var perms = _perm.GetUserPermissions(user.Id);
        Assert.Empty(perms);
    }

    [Fact]
    public void GetAllUserRoles_ShouldListAllUsers()
    {
        _db.Register("listuser1", "pass123");
        _db.Register("listuser2", "pass123");
        var list = _perm.GetAllUserRoles();
        var names = list.Select(u => u.Username).ToList();
        Assert.Contains("listuser1", names);
        Assert.Contains("listuser2", names);
    }
}
