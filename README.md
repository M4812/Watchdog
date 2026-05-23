# Watchdog Tool

Watchdog Tool 是一个 Windows 桌面看门狗工具，用于守护指定的 `.exe` 进程。它会按固定间隔检查目标进程状态，在进程退出或带窗口程序无响应时自动拉起或重启，并在界面中保留运行日志。

当前发布版本：`v1.0`

## 产品截图

![Watchdog Tool 软件截图](docs/images/watchdog-tool.png)

## 适用场景

- 守护本地业务程序、采集程序、后台任务或简单服务进程。
- 在没有完整服务管理平台的 Windows 机器上，为关键 EXE 增加自动恢复能力。
- 需要人工可视化观察守护状态、最近恢复记录和错误日志的桌面环境。

## 核心功能

- 选择需要监控的 `.exe` 文件。
- 配置检查间隔和无响应判定时间。
- 手动开始监控、停止监控、立即检查。
- 目标进程退出后自动重新启动。
- 带窗口的 GUI 进程无响应时自动结束并重启。
- 后台或服务器型无窗口进程按运行状态监控，避免误判为无响应。
- 运行日志按时间、类型、内容分列显示，最多保留 500 条。

## 运行要求

- Windows 10、Windows Server 2019 或更新版本。
- .NET 9 Desktop Runtime。

v1.0 默认发布包为框架依赖版本，因此目标机器需要安装 .NET 9 Desktop Runtime。也可以按下方命令自行构建自包含发布包。

## 使用方式

1. 下载并解压 v1.0 发布包。
2. 打开 `Watchdog.App.exe`。
3. 点击 `选择 EXE`，选择需要守护的目标程序。
4. 设置 `检查间隔` 和 `无响应判定`。
5. 点击 `开始监控`。
6. 需要调整配置时，先点击 `停止监控`。

## 日志类型

- `信息`：正常检查、状态提示。
- `警告`：进程退出、未发现进程、进程无响应。
- `恢复`：进程启动或重启成功。
- `错误`：检查过程出现异常。

## 从源码构建

```powershell
dotnet restore
dotnet build WatchdogTool.sln
dotnet test WatchdogTool.sln
dotnet publish src\Watchdog.App\Watchdog.App.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64
```

发布后运行：

```powershell
publish\win-x64\Watchdog.App.exe
```

构建自包含版本：

```powershell
dotnet publish src\Watchdog.App\Watchdog.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\win-x64-self-contained
```

## 项目结构

- `src/Watchdog.App`：WinForms 桌面应用。
- `src/Watchdog.Core`：进程监控核心逻辑。
- `tests/Watchdog.Core.Tests`：核心逻辑测试。
- `docs/images`：产品截图和文档图片。

## 开发质量

- 使用 `.editorconfig` 统一 C#、项目文件和 Markdown 基础格式。
- 使用 `.gitattributes` 固定文本文件换行，减少跨平台无意义 diff。
- 核心监控逻辑包含 MSTest 单元测试。
