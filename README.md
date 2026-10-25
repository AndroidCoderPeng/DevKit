# DevKit · 开发者工具箱

> 一个面向移动端 / 嵌入式 / 网络开发的 Windows 桌面工具集，集成 ADB 调试、串口通信、TCP/UDP/WebSocket 调试、颜色处理、网络诊断等常用功能。

> 📷 **截图待补**：软件主界面（建议放 `Screenshot/main.png`，窗口约 408×828）

---

## ✨ 功能一览

DevKit 按用途将工具分为 **Android 工具**、**通信工具**、**其他工具** 三大类，点击主界面卡片即可打开对应功能窗口。

### Android 工具

| 工具 | 说明 |
| --- | --- |
| **ADB** | 连接 Android 设备后查看设备详情（品牌/型号/系统版本/CPU/分辨率/电池/Android ID/IP 等）；支持安装/卸载/导出 APK、截屏、文件传输、Logcat 抓取、设备重启 |
| **APK** | 配置 JDK 与签名 Key，查看 APK 证书 SHA1 信息；扫描本地 APK 归档目录并解析包名、构建时间、版本号 |
| **JNI 逆向** | 基于 NDK 的 `addr2line` 工具，将 `.so` 崩溃堆栈地址还原为函数名与行号；自动识别 NDK 版本与目标 ABI（armeabi-v7a / arm64-v8a / x86 / x86_64） |

> 📷 **截图待补**（放 `Screenshot/` 目录，下同）：
> - ADB 设备详情页 → `Screenshot/adb.png`
> - APK 证书 / 扫描页 → `Screenshot/apk.png`
> - JNI 逆向页 → `Screenshot/jni.png`

### 通信工具

提供一致的交互体验（HEX/ASCII 收发、扩展指令、脚本发送、循环发送、日志保存等）：

| 工具 | 说明 |
| --- | --- |
| **TCP 客户端 / 服务端** | 基于 TouchSocket 的 TCP 通信调试 |
| **UDP 客户端 / 服务端** | UDP 收发调试 |
| **WS 客户端 / 服务端** | WebSocket 通信调试 |
| **串口通信** | 配置端口、波特率、数据位/停止位/校验/流控；支持扩展指令、脚本发送、循环发送、会话统计、HEX/ASCII 切换 |

> 📷 **截图待补**：
> - TCP 客户端通信页 → `Screenshot/tcp.png`
> - 串口通信页 → `Screenshot/serial.png`
> - WebSocket 通信页 → `Screenshot/ws.png`

### 其他工具

| 工具 | 说明 |
| --- | --- |
| **颜色处理** | RGB ⇄ HEX 互转（支持 Alpha），实时取色、随机色、最近使用色、内置颜色资源库搜索 |
| **网络配置** | 查看本机各网卡 IPv4/子网掩码/网关/MAC/DNS/连接状态/带宽；内置 `ipconfig`、`ping`、`tracert`、`nslookup`、`netstat` 等常用网络命令终端 |

> 📷 **截图待补**：
> - 颜色处理页 → `Screenshot/color.png`
> - 网络配置页 → `Screenshot/netconfig.png`

### 通用能力

- **置顶显示**：主窗口支持一键置顶，方便边调试边看日志。
- **扩展指令**：通信类工具可保存常用指令（HEX / ASCII），一键快捷发送。
- **指令脚本**：按顺序批量发送多条指令，可配置每条之间的时间间隔。
- **循环发送**：按指定间隔自动重复发送当前内容。
- **日志保存 / 清空**：通信日志支持以 `时间【发送/接收】内容` 格式导出为 `.txt`。

---

## 🧱 技术栈

- **平台**：Windows 桌面应用（WPF）
- **框架**：.NET 8.0 (`net8.0-windows`)，`UseWPF` + `UseWindowsForms`
- **架构**：MVVM（[CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)）
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **界面行为**：Microsoft.Xaml.Behaviors.Wpf
- **JSON**：Newtonsoft.Json
- **串口**：System.IO.Ports
- **网络通信**：[TouchSocket](https://www.nuget.org/packages/TouchSocket)（TCP / UDP / WebSocket / 串口）
- **图标字体**：内置 `iconfont.ttf`

---

## 📂 项目结构

```
DevKit/
├── DevKit.sln                # 解决方案
├── DevKit/                   # WPF 主工程（C#）
│   ├── Views/                # 各功能页面（.xaml / .xaml.cs）
│   ├── ViewModels/           # 对应 MVVM 视图模型
│   ├── Models/               # 数据模型（MainMenu、SocketMessage 等）
│   ├── Dialogs/              # 弹窗（Logcat、文件传输、指令脚本等）
│   ├── Transports/           # 通信传输层（TCP/UDP/WS/串口）
│   ├── DataService/          # 数据服务（如本机 IP 获取）
│   ├── Cache/                # 配置缓存读写（SettingsStore）
│   ├── Utils/                # 命令执行、参数构造等工具类
│   ├── Styles/ Colors/ Fonts/ Images/   # 样式、颜色资源、字体、图标
│   ├── App.xaml(.cs)
│   ├── MainWindow.xaml(.cs)  # 主窗口（工具卡片导航）
│   └── DevKit.csproj
├── Design/                   # 前端原型（HTML/CSS/JS），与 WPF 页面一一对应
├── Shell/                    # 打包脚本（Inno Setup + 发布 bat）
│   ├── DevKit.iss            # Inno Setup 安装包配置
│   └── build_release_script.bat
├── Screenshot/               # 截图资源（README 配图放这里）
└── LICENSE                   # MIT License
```

---

## 🚀 构建与运行

### 环境要求

- Windows 10 / 11（x64）
- [.NET 8.0 运行时 / SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- 使用 ADB 功能需自行配置 `adb` 并加入 PATH；JNI 逆向需安装 Android NDK

### 使用源码运行

```bash
# 进入主工程目录
cd DevKit/DevKit

# 还原依赖并启动（Debug）
dotnet restore
dotnet run -c Debug
```

### 发布为独立可执行文件

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

发布产物位于 `DevKit/DevKit/bin/Release/`。

---

## 📜 许可证

本项目基于 [MIT License](./LICENSE) 开源。Copyright (c) 2024 CoderPeng。
