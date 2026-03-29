using WmsCore.Database;
using WmsWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加 Blazor Server 服务
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 添加 API 控制器（供 OMS 调用）
builder.Services.AddControllers();

// 跨域支持（OMS 与 WMS 可能在不同域名部署）
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// 数据库路径（服务端）
var dbPath = Path.Combine(AppContext.BaseDirectory, "wms.db");

// 核心数据库服务（Singleton）
builder.Services.AddSingleton<DatabaseHelper>(_ => new DatabaseHelper(dbPath));
builder.Services.AddSingleton<PermissionHelper>(_ => new PermissionHelper(dbPath));

// 用户会话状态（Scoped：每个 Blazor Circuit 独立）
builder.Services.AddScoped<AuthState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseCors();
app.UseStaticFiles();
app.UseRouting();

// API 路由（供 OMS 调用）
app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
