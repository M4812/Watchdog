# Watchdog Tool

Watchdog Tool 是一个 Windows 桌面看门狗工具，用于守护指定的 `.exe` 进程。它会按固定间隔检查目标进程状态，在进程退出、未启动、带窗口程序无响应，或可选的文件夹文件堆积超时时自动启动或重启目标程序，并在界面与本地日志中记录恢复事件。

当前版本：`v1.1.1`

## 产品截图

![Watchdog Tool 截图](docs/images/watchdog-tool-v1.1.1.png)

## 核心功能

- 选择需要监控的 `.exe` 文件。
- 配置检查间隔和无响应判定时间。
- 可选启用文件夹堆积监控：指定文件夹内持续存在文件超过设定秒数时，自动重启被监控程序。
- 手动开始监控、停止监控、立即检查。
- 目标进程退出或未发现时自动启动。
- GUI 进程无响应时自动结束并重启。
- 后台或服务型无窗口进程按运行状态监控。
- 界面运行日志最多保留 500 条。
- 恢复类事件会写入程序同级目录 `log\watchdog-yyyyMMdd.log`。

## 发布包

v1.1.1 提供 Windows 发布包：

- `Watchdog-v1.1.1-net9.0-win-x64-self-contained.zip`  
  .NET 9 自包含版，适合常规 64 位 Windows 环境，不需要额外安装 .NET 9 Runtime。

## 使用方式

1. 解压对应发布包。
2. 打开 `Watchdog.App.exe`。
3. 点击 `选择 EXE`，选择需要守护的目标程序。
4. 设置 `检查间隔` 和 `无响应判定`。
5. 如果需要文件夹堆积触发重启，勾选 `启用文件夹堆积监控`，选择文件夹并设置 `堆积判定` 秒数。
6. 点击 `开始监控`。
7. 如需修改配置，先点击 `停止监控`。

## 日志说明

界面日志分为：

- `信息`：正常检查与状态提示。
- `警告`：未发现进程、进程退出、进程无响应、文件夹堆积。
- `恢复`：启动或重启成功。
- `错误`：检查或恢复过程中的异常。

恢复类事件会额外写入本地文件：

```text
log\watchdog-yyyyMMdd.log
```

## 从源码构建

```powershell
dotnet restore
dotnet build WatchdogTool.sln
dotnet test WatchdogTool.sln
```

发布 .NET 9 自包含版：

```powershell
dotnet publish src\Watchdog.App\Watchdog.App.csproj -f net9.0-windows -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\v1.1.1-net9.0-win-x64-self-contained
```

## 项目结构

- `src/Watchdog.App`：WinForms 桌面应用。
- `src/Watchdog.Core`：进程监控核心逻辑。
- `tests/Watchdog.Core.Tests`：核心逻辑测试。
- `tests/Watchdog.App.Tests`：WinForms 界面布局测试。
- `docs/images`：产品截图和文档图片。
