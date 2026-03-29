using WmsCore.Database;

namespace WmsWeb.Services;

/// <summary>
/// 当前登录用户的会话状态（Scoped，每个 Blazor 连接独立）
/// </summary>
public class AuthState
{
    public UserInfo?      CurrentUser { get; private set; }
    public HashSet<string> Permissions { get; private set; } = new();
    public bool           IsLoggedIn  => CurrentUser != null;

    public event Action? OnChanged;

    public void Login(UserInfo user, HashSet<string> permissions)
    {
        CurrentUser = user;
        Permissions = permissions;
        OnChanged?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        Permissions = new HashSet<string>();
        OnChanged?.Invoke();
    }

    /// <summary>用户是否拥有指定权限点</summary>
    public bool Has(string permissionCode)
        => Permissions.Contains(permissionCode);

    /// <summary>用户是否拥有任意一个权限点（OR 条件）</summary>
    public bool HasAny(params string[] codes)
        => codes.Any(c => Permissions.Contains(c));
}
