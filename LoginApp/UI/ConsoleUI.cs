using LoginApp.Database;

namespace LoginApp.UI;

/// <summary>
/// 控制台用户界面：主菜单、登录、注册流程
/// </summary>
public class ConsoleUI
{
    private readonly DatabaseHelper _db;

    public ConsoleUI()
    {
        _db = new DatabaseHelper();
    }

    public void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        PrintBanner();

        while (true)
        {
            PrintMainMenu();
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    LoginFlow();
                    break;
                case "2":
                    RegisterFlow();
                    break;
                case "0":
                    PrintInfo("再见！感谢使用。");
                    return;
                default:
                    PrintWarning("无效选项，请输入 0、1 或 2");
                    break;
            }
        }
    }

    // ─── 登录流程 ────────────────────────────────────────────────
    private void LoginFlow()
    {
        Console.WriteLine();
        PrintBox("用 户 登 录");

        var username = ReadInput("用户名");
        var password = ReadPassword("密  码");

        var (success, message) = _db.Login(username, password);

        if (success)
        {
            PrintSuccess($"\n  ✓ 欢迎回来，{username}！{message}");
        }
        else
        {
            PrintError($"\n  ✗ 登录失败：{message}");
        }

        PauseAndContinue();
    }

    // ─── 注册流程 ────────────────────────────────────────────────
    private void RegisterFlow()
    {
        Console.WriteLine();
        PrintBox("新 用 户 注 册");

        var username = ReadInput("用户名（≥3位）");
        var password = ReadPassword("密  码（≥6位）");
        var confirm  = ReadPassword("确认密码      ");

        if (password != confirm)
        {
            PrintError("\n  ✗ 两次密码输入不一致，请重新注册");
            PauseAndContinue();
            return;
        }

        var email = ReadInput("邮  箱（可选，直接回车跳过）", optional: true);

        var (success, message) = _db.Register(username, password, email);

        if (success)
            PrintSuccess($"\n  ✓ {message}请返回主菜单登录。");
        else
            PrintError($"\n  ✗ 注册失败：{message}");

        PauseAndContinue();
    }

    // ─── 辅助方法 ─────────────────────────────────────────────────
    private static string ReadInput(string label, bool optional = false)
    {
        while (true)
        {
            Console.Write($"  {label,-18}: ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(input) || optional)
                return input;
            PrintWarning("  输入不能为空，请重新输入");
        }
    }

    /// <summary>输入密码时隐藏字符（显示 *）</summary>
    private static string ReadPassword(string label)
    {
        Console.Write($"  {label,-18}: ");
        var sb = new System.Text.StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        return sb.ToString();
    }

    private static void PauseAndContinue()
    {
        Console.WriteLine();
        Console.Write("  按任意键返回主菜单...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }

    // ─── 样式输出 ─────────────────────────────────────────────────
    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ╔══════════════════════════════════════╗");
        Console.WriteLine("  ║       用户登录系统  v1.0             ║");
        Console.WriteLine("  ║       C# + SQLite                    ║");
        Console.WriteLine("  ╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintMainMenu()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  ┌─────────────── 主菜单 ───────────────┐");
        Console.WriteLine("  │  1. 登录                             │");
        Console.WriteLine("  │  2. 注册新用户                       │");
        Console.WriteLine("  │  0. 退出                             │");
        Console.WriteLine("  └──────────────────────────────────────┘");
        Console.ResetColor();
        Console.Write("  请选择 > ");
    }

    private static void PrintBox(string title)
    {
        var pad = new string('─', 38 - title.Length);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ┌── {title} {pad}┐");
        Console.ResetColor();
    }

    private static void PrintSuccess(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    private static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    private static void PrintWarning(string msg)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    private static void PrintInfo(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  {msg}");
        Console.ResetColor();
    }
}
