using OmsCore.Database;

namespace OmsWeb.Services;

/// <summary>
/// OMS 当前登录用户会话状态（Scoped，每个 Blazor 连接独立）
/// </summary>
public class OmsAuthState
{
    public OmsUserInfo?     CurrentUser { get; private set; }
    public HashSet<string>  Permissions { get; private set; } = new();
    public bool             IsLoggedIn  => CurrentUser != null;

    public event Action? OnChanged;

    public void Login(OmsUserInfo user, HashSet<string> permissions)
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

    public bool Has(string code)         => Permissions.Contains(code);
    public bool HasAny(params string[] codes) => codes.Any(c => Permissions.Contains(c));
}
