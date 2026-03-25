# WMSbyWorkBuddy - 仓库管理系统

> 由 WorkBuddy AI 辅助开发的 WMS（仓库管理系统）原型，包含 Web 后台和 PDA 移动终端。

## 项目结构

```
├── WmsCore/          # 共享类库（数据库层、用户模型）
├── WmsWeb/           # Web 后台（Blazor WebAssembly）
├── WmsPda/           # PDA 移动端（.NET MAUI，Android）
├── WmsTests/         # 单元测试（xUnit，19个测试用例）
└── LoginApp/         # 控制台原型（早期版本）
```

## 技术栈

| 模块 | 技术 |
|------|------|
| Web 后台 | Blazor WebAssembly + .NET 10 |
| PDA 移动端 | .NET MAUI（Android）|
| 共享数据库 | SQLite + WmsCore 类库 |
| 测试 | xUnit |
| 密码安全 | SHA-256 哈希 |

## 已实现功能

- [x] 用户注册（用户名唯一、密码哈希、邮箱可选）
- [x] 用户登录（Web + PDA 双端）
- [x] Web 仪表盘（数据概览）
- [x] PDA 大字体/大按钮界面（适配扫描枪设备）

## 安装与运行

### Web 后台

```bash
cd WmsWeb
dotnet run
```

浏览器访问 `http://localhost:5000`

### PDA（Android APK）

已编译的 APK 位于：
```
WmsPda/bin/Release/net10.0-android/publish/com.wms.pda-Signed.apk
```

直接传至 Android 设备安装即可（需允许未知来源）。

### 重新编译 APK

```bash
export JAVA_HOME=/Library/Java/JavaVirtualMachines/zulu-17.jdk/Contents/Home
export ANDROID_HOME=/opt/homebrew/share/android-commandlinetools
cd WmsPda
dotnet publish -f net10.0-android -c Release
```

### 运行测试

```bash
cd WmsTests
dotnet test
```

## 待开发功能

- [ ] 仓库 / 货架 / 库位管理
- [ ] 商品 SKU 管理
- [ ] 入库 / 出库流程
- [ ] 库存查询

---

*Powered by WorkBuddy AI*
