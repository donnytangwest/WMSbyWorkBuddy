using WmsCore.Database;

var builder = WebApplication.CreateBuilder(args);

// 添加 Blazor Server 服务
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 注册数据库（单例，SQLite 文件存在服务端）
var dbPath = Path.Combine(AppContext.BaseDirectory, "wms.db");
builder.Services.AddSingleton<DatabaseHelper>(_ => new DatabaseHelper(dbPath));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
