using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace WmsCore.Database;

/// <summary>
/// 数据库辅助类：负责初始化 SQLite 数据库、用户 CRUD 操作
/// 被 WmsWeb（Blazor）和 WmsPda（MAUI）共同引用
/// </summary>
public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper(string dbPath = "wms.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库，创建用户表（如不存在）
    /// </summary>
    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            CREATE TABLE IF NOT EXISTS Users (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Username  TEXT    NOT NULL UNIQUE,
                Password  TEXT    NOT NULL,
                Email     TEXT    NOT NULL DEFAULT '',
                Role      TEXT    NOT NULL DEFAULT 'operator',
                CreatedAt TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """;

        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 注册新用户
    /// </summary>
    public (bool Success, string Message) Register(string username, string password, string email = "", string role = "operator")
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "用户名至少需要 3 个字符");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "密码至少需要 6 个字符");

        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            const string sql = "INSERT INTO Users (Username, Password, Email, Role) VALUES (@u, @p, @e, @r)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username.Trim());
            cmd.Parameters.AddWithValue("@p", HashPassword(password));
            cmd.Parameters.AddWithValue("@e", email.Trim());
            cmd.Parameters.AddWithValue("@r", role);
            cmd.ExecuteNonQuery();

            return (true, "注册成功！");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, "该用户名已被占用，请换一个");
        }
        catch (Exception ex)
        {
            return (false, $"注册失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证登录，返回成功/失败及用户信息
    /// </summary>
    public (bool Success, string Message, UserInfo? User) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "用户名或密码不能为空", null);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = "SELECT Id, Username, Password, Role FROM Users WHERE Username = @u";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username.Trim());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return (false, "用户名不存在", null);

        var storedHash = reader.GetString(2);
        if (HashPassword(password) != storedHash)
            return (false, "密码错误，请重试", null);

        var user = new UserInfo
        {
            Id       = reader.GetInt32(0),
            Username = reader.GetString(1),
            Role     = reader.GetString(3)
        };

        return (true, "登录成功！", user);
    }

    /// <summary>
    /// SHA-256 密码哈希
    /// </summary>
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}

/// <summary>
/// 登录成功后的用户信息
/// </summary>
public class UserInfo
{
    public int    Id       { get; set; }
    public string Username { get; set; } = "";
    public string Role     { get; set; } = "operator";
}
