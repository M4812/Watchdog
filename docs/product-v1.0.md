# Watchdog Tool v1.0 产品文档

Watchdog Tool v1.0 是一个面向 Windows 桌面的进程守护工具。用户选择一个目标 EXE 后，工具会按配置的检查间隔持续观察进程状态，并在目标退出或无响应时自动恢复。

## 产品截图

![Watchdog Tool v1.0 截图](images/watchdog-tool.png)

## 功能概览

- 通过文件选择器指定需要守护的 `.exe`。
- 配置检查间隔，控制监控频率。
- 配置无响应判定时间，用于处理带窗口 GUI 程序卡死。
- 支持开始监控、停止监控、立即检查。
- 进程退出时自动启动。
- GUI 程序无响应时自动结束并重启。
- 无窗口后台进程按运行状态监控，避免误杀。
- 日志按时间、类型、内容展示，最多保留 500 条。

## 安装与运行

1. 下载 `Watchdog.Tool-v1.0-win-x64.zip`。
2. 解压到本地目录。
3. 确认机器已安装 .NET 9 Desktop Runtime。
4. 双击运行 `Watchdog.App.exe`。

## 操作流程

1. 点击 `选择 EXE`，选择要守护的程序。
2. 设置 `检查间隔`。建议从 10 秒开始，根据目标程序的重要性和资源占用调整。
3. 设置 `无响应判定`。GUI 程序建议设置为 5 秒或更长；后台程序不会因为没有窗口响应而被误判。
4. 点击 `开始监控`。
5. 观察底部运行日志，确认工具已开始检查目标进程。
6. 如需修改配置，先点击 `停止监控`。

## 日志说明

- `信息`：正常状态或常规提示。
- `警告`：未发现进程、进程退出或进程无响应。
- `恢复`：工具已启动或重启目标进程。
- `错误`：检查或恢复过程中出现异常。

## 运行要求

- Windows 10、Windows Server 2019 或更新版本。
- .NET 9 Desktop Runtime。
- 当前 Windows 用户需要具备启动和结束目标进程的权限。

## 源码构建

```powershell
dotnet restore
dotnet build WatchdogTool.sln
dotnet test WatchdogTool.sln
dotnet publish src\Watchdog.App\Watchdog.App.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64
```

## 已知限制

- v1.0 不包含后台 Windows Service 模式，关闭桌面程序后监控会停止。
- v1.0 不保存历史配置，下次启动需要重新选择目标 EXE。
- v1.0 不提供远程告警或通知能力。
