using OmsCore.Api;
using OmsCore.Database;
using OmsWeb.Components;
using OmsWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 数据库路径
var dbPath = Path.Combine(AppContext.BaseDirectory, "oms.db");

// OMS 核心服务（Singleton）
builder.Services.AddSingleton<OmsDatabaseHelper>(_ => new OmsDatabaseHelper(dbPath));
builder.Services.AddSingleton<OmsPermissionHelper>(_ => new OmsPermissionHelper(dbPath));

// WMS API 客户端（Singleton，地址从配置读取）
var wmsUrl = builder.Configuration["WmsApiUrl"] ?? "http://localhost:5188";
builder.Services.AddSingleton<WmsApiClient>(_ => new WmsApiClient(wmsUrl));

// 用户会话状态（Scoped：每个 Blazor 连接独立）
builder.Services.AddScoped<OmsAuthState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
