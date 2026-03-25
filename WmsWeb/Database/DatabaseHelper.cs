using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace WmsWeb.Database;

/// <summary>
/// 数据库辅助类：负责初始化 SQLite 数据库、用户 CRUD 操作
/// </summary>
public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper(string dbPath = "users.db")
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
                Password  TEXT    NOT NULL,   -- 存储 SHA-256 哈希值
                Email     TEXT,
                CreatedAt TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            );
        """;

        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 注册新用户，返回是否成功
    /// </summary>
    public (bool Success, string Message) Register(string username, string password, string email = "")
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "用户名至少需要 3 个字符");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "密码至少需要 6 个字符");

        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            const string sql = "INSERT INTO Users (Username, Password, Email) VALUES (@u, @p, @e)";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username.Trim());
            cmd.Parameters.AddWithValue("@p", HashPassword(password));
            cmd.Parameters.AddWithValue("@e", email.Trim());
            cmd.ExecuteNonQuery();

            return (true, "注册成功！");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint
        {
            return (false, "该用户名已被占用，请换一个");
        }
        catch (Exception ex)
        {
            return (false, $"注册失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证登录，返回是否成功及提示信息
    /// </summary>
    public (bool Success, string Message) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "用户名或密码不能为空");

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = "SELECT Password FROM Users WHERE Username = @u";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username.Trim());

        var storedHash = cmd.ExecuteScalar() as string;
        if (storedHash is null)
            return (false, "用户名不存在");

        return HashPassword(password) == storedHash
            ? (true, "登录成功！")
            : (false, "密码错误，请重试");
    }

    /// <summary>
    /// SHA-256 密码哈希
    /// </summary>
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
