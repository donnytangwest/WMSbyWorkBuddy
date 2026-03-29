namespace WmsCore.Database;

/// <summary>
/// 权限类型：菜单、子菜单、功能按钮
/// </summary>
public enum PermissionType
{
    Menu     = 1,   // 一级菜单
    SubMenu  = 2,   // 子菜单
    Button   = 3,   // 页面内功能按钮
}

/// <summary>
/// 权限点：系统中所有可配置的权限项（不写死，存入数据库可动态增减）
/// </summary>
public class PermissionItem
{
    public int            Id          { get; set; }
    /// <summary>权限编码，全局唯一，如 menu.warehouse / btn.inbound.create</summary>
    public string         Code        { get; set; } = "";
    /// <summary>显示名称，如"仓库管理"、"新增入库单"</summary>
    public string         Name        { get; set; } = "";
    /// <summary>所属模块/分组，用于 UI 分组展示</summary>
    public string         Group       { get; set; } = "";
    /// <summary>权限类型</summary>
    public PermissionType Type        { get; set; } = PermissionType.Menu;
    /// <summary>父级权限编码，顶级菜单为空</summary>
    public string         ParentCode  { get; set; } = "";
    /// <summary>排序号</summary>
    public int            SortOrder   { get; set; } = 0;
    /// <summary>描述</summary>
    public string         Description { get; set; } = "";
}

/// <summary>
/// 角色
/// </summary>
public class Role
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = "";
    public string Code        { get; set; } = "";  // 唯一标识，如 admin / operator
    public string Description { get; set; } = "";
    public string CreatedAt   { get; set; } = "";
    /// <summary>该角色拥有的权限编码列表（查询时填充，不存库）</summary>
    public List<string> PermissionCodes { get; set; } = new();
}

/// <summary>
/// 用户角色分配信息（查询用）
/// </summary>
public class UserRoleInfo
{
    public int    UserId    { get; set; }
    public string Username  { get; set; } = "";
    /// <summary>该用户被分配的角色编码列表（查询时填充）</summary>
    public List<string> RoleCodes { get; set; } = new();
}
