using WmsCore.Database;

namespace WmsPda.Services;

/// <summary>
/// 认证服务：封装登录/注册逻辑，管理当前登录用户状态
/// </summary>
public class AuthService
{
    private readonly DatabaseHelper _db;

    /// <summary>当前已登录用户，未登录时为 null</summary>
    public UserInfo? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public AuthService()
    {
        // 数据库文件存到应用数据目录
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wms.db");
        _db = new DatabaseHelper(dbPath);
    }

    /// <summary>
    /// 登录
    /// </summary>
    public (bool Success, string Message) Login(string username, string password)
    {
        var (success, message, user) = _db.Login(username, password);
        if (success && user != null)
            CurrentUser = user;
        return (success, message);
    }

    /// <summary>
    /// 注册
    /// </summary>
    public (bool Success, string Message) Register(string username, string password, string email = "")
        => _db.Register(username, password, email);

    /// <summary>
    /// 登出
    /// </summary>
    public void Logout() => CurrentUser = null;
}
