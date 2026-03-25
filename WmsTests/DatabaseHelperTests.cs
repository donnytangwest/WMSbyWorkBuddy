using WmsCore.Database;

namespace WmsTests;

/// <summary>
/// DatabaseHelper 单元测试
/// 每个测试使用独立的临时数据库文件，互不干扰
/// </summary>
public class DatabaseHelperTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DatabaseHelper _db;

    public DatabaseHelperTests()
    {
        // 每个测试用唯一临时文件，避免状态污染
        _dbPath = Path.Combine(Path.GetTempPath(), $"wms_test_{Guid.NewGuid():N}.db");
        _db = new DatabaseHelper(_dbPath);
    }

    public void Dispose()
    {
        // 测试结束清理数据库文件
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    // ─── 注册测试 ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Register")]
    public void Register_ValidUser_ShouldSucceed()
    {
        var (success, msg) = _db.Register("testuser", "password123");
        Assert.True(success, $"期望注册成功，实际：{msg}");
        Assert.Contains("成功", msg);
    }

    [Fact]
    [Trait("Category", "Register")]
    public void Register_DuplicateUsername_ShouldFail()
    {
        _db.Register("dupuser", "password123");
        var (success, msg) = _db.Register("dupuser", "anotherpass");
        Assert.False(success, "重复用户名应该注册失败");
        Assert.Contains("已被占用", msg);
    }

    [Theory]
    [Trait("Category", "Register")]
    [InlineData("ab", "password123")]      // 用户名太短
    [InlineData("validuser", "12345")]     // 密码太短
    [InlineData("", "password123")]        // 用户名为空
    [InlineData("validuser", "")]          // 密码为空
    public void Register_InvalidInput_ShouldFail(string username, string password)
    {
        var (success, _) = _db.Register(username, password);
        Assert.False(success, $"用户名='{username}' 密码='{password}' 应该注册失败");
    }

    [Fact]
    [Trait("Category", "Register")]
    public void Register_WithEmail_ShouldSucceed()
    {
        var (success, msg) = _db.Register("emailuser", "password123", "test@example.com");
        Assert.True(success, $"带邮箱注册失败：{msg}");
    }

    // ─── 登录测试 ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Login")]
    public void Login_CorrectCredentials_ShouldSucceed()
    {
        _db.Register("loginuser", "mypassword");

        var (success, msg, user) = _db.Login("loginuser", "mypassword");

        Assert.True(success, $"期望登录成功，实际：{msg}");
        Assert.NotNull(user);
        Assert.Equal("loginuser", user.Username);
    }

    [Fact]
    [Trait("Category", "Login")]
    public void Login_WrongPassword_ShouldFail()
    {
        _db.Register("pwduser", "correctpass");

        var (success, msg, user) = _db.Login("pwduser", "wrongpass");

        Assert.False(success, "密码错误应该登录失败");
        Assert.Null(user);
        Assert.Contains("密码错误", msg);
    }

    [Fact]
    [Trait("Category", "Login")]
    public void Login_NonExistentUser_ShouldFail()
    {
        var (success, msg, user) = _db.Login("nobody", "password123");

        Assert.False(success, "不存在的用户应该登录失败");
        Assert.Null(user);
        Assert.Contains("不存在", msg);
    }

    [Theory]
    [Trait("Category", "Login")]
    [InlineData("", "password")]
    [InlineData("username", "")]
    [InlineData("", "")]
    public void Login_EmptyCredentials_ShouldFail(string username, string password)
    {
        var (success, _, user) = _db.Login(username, password);
        Assert.False(success, "空用户名或密码应该登录失败");
        Assert.Null(user);
    }

    [Fact]
    [Trait("Category", "Login")]
    public void Login_ReturnsCorrectUserInfo()
    {
        _db.Register("infouser", "pass123456");

        var (success, _, user) = _db.Login("infouser", "pass123456");

        Assert.True(success);
        Assert.NotNull(user);
        Assert.True(user.Id > 0);
        Assert.Equal("infouser", user.Username);
        Assert.Equal("operator", user.Role); // 默认角色
    }

    // ─── 密码安全测试 ────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Security")]
    public void Password_ShouldBeHashed_NotStoredInPlaintext()
    {
        _db.Register("hashuser", "plaintext");

        // 直接读 SQLite 文件内容，不应包含明文密码
        var dbContent = File.ReadAllText(_dbPath);
        Assert.DoesNotContain("plaintext", dbContent);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void HashPassword_SameInput_ShouldProduceSameHash()
    {
        var hash1 = DatabaseHelper.HashPassword("mypassword");
        var hash2 = DatabaseHelper.HashPassword("mypassword");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void HashPassword_DifferentInputs_ShouldProduceDifferentHash()
    {
        var hash1 = DatabaseHelper.HashPassword("password1");
        var hash2 = DatabaseHelper.HashPassword("password2");
        Assert.NotEqual(hash1, hash2);
    }

    // ─── 并发/边界测试 ────────────────────────────────────────────

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void Register_UsernameWithSpaces_ShouldTrim()
    {
        var (success, _) = _db.Register("  spaceuser  ", "password123");
        Assert.True(success, "前后带空格的用户名应该注册成功（trim 后）");

        // 用 trim 后的用户名能登录
        var (loginSuccess, _, _) = _db.Login("spaceuser", "password123");
        Assert.True(loginSuccess, "trim 后的用户名应该能登录");
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void MultipleUsers_CanRegisterAndLoginIndependently()
    {
        _db.Register("user1", "pass111111");
        _db.Register("user2", "pass222222");

        var (ok1, _, u1) = _db.Login("user1", "pass111111");
        var (ok2, _, u2) = _db.Login("user2", "pass222222");

        Assert.True(ok1);
        Assert.True(ok2);
        Assert.NotEqual(u1!.Id, u2!.Id);
    }
}
