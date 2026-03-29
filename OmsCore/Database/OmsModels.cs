namespace OmsCore.Database;

// ──────────────────────────────────────
//  用户信息
// ──────────────────────────────────────
public class OmsUserInfo
{
    public int    Id       { get; set; }
    public string Username { get; set; } = "";
    public string Email    { get; set; } = "";
}

// ──────────────────────────────────────
//  权限类型
// ──────────────────────────────────────
public enum OmsPermissionType
{
    Menu    = 1,
    SubMenu = 2,
    Button  = 3,
}

// ──────────────────────────────────────
//  权限点
// ──────────────────────────────────────
public class OmsPermissionItem
{
    public int               Id          { get; set; }
    public string            Code        { get; set; } = "";
    public string            Name        { get; set; } = "";
    public string            Group       { get; set; } = "";
    public OmsPermissionType Type        { get; set; } = OmsPermissionType.Menu;
    public string            ParentCode  { get; set; } = "";
    public int               SortOrder   { get; set; } = 0;
    public string            Description { get; set; } = "";
}

// ──────────────────────────────────────
//  角色
// ──────────────────────────────────────
public class OmsRole
{
    public int    Id              { get; set; }
    public string Name            { get; set; } = "";
    public string Code            { get; set; } = "";
    public string Description     { get; set; } = "";
    public string CreatedAt       { get; set; } = "";
    public List<string> PermissionCodes { get; set; } = new();
}

// ──────────────────────────────────────
//  用户角色信息（查询用）
// ──────────────────────────────────────
public class OmsUserRoleInfo
{
    public int    UserId    { get; set; }
    public string Username  { get; set; } = "";
    public List<string> RoleCodes { get; set; } = new();
}
