# 看门狗工具初版 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Windows WinForms 桌面工具，可选择 exe 进程并在进程退出或无响应时自动重启，同时在界面显示日志并带有狗狗图标。

**Architecture:** 使用 .NET 9 WinForms 作为桌面界面，核心进程监控逻辑放在独立类库中，方便测试。UI 仅负责选择 exe、配置间隔、启动/停止、订阅日志和状态变化。

**Tech Stack:** C#、.NET 9、WinForms、MSTest。

---

## File Structure

- `WatchdogTool.sln`: 解决方案。
- `src/Watchdog.Core/ProcessWatchdog.cs`: 核心守护服务，管理启动、检测、重启、日志事件。
- `src/Watchdog.Core/WatchdogOptions.cs`: 监控配置。
- `src/Watchdog.Core/IProcessController.cs`: 进程操作抽象，便于单元测试。
- `src/Watchdog.Core/SystemProcessController.cs`: 真实 Windows 进程控制实现。
- `tests/Watchdog.Core.Tests/ProcessWatchdogTests.cs`: 核心行为测试。
- `src/Watchdog.App/MainForm.cs`: WinForms 界面。
- `src/Watchdog.App/Program.cs`: 应用入口。
- `src/Watchdog.App/IconFactory.cs`: 代码生成狗狗图标。

## Tasks

### Task 1: Solution Scaffold

- [ ] Create solution, class library, WinForms app, MSTest project.
- [ ] Add references from app/tests to core.
- [ ] Verify `dotnet test` sees the projects.

### Task 2: Core TDD

- [ ] Write failing tests for starting missing process, restarting exited process, restarting unresponsive process, and stopping monitoring.
- [ ] Implement `IProcessController`, `WatchdogOptions`, and `ProcessWatchdog` minimally.
- [ ] Run tests until green.

### Task 3: WinForms UI

- [ ] Build UI with exe picker, process name display, interval input, start/stop buttons, status label, and log list.
- [ ] Wire UI to `ProcessWatchdog` events.
- [ ] Generate and apply dog icon through `IconFactory`.

### Task 4: Verification and Publish

- [ ] Run `dotnet test`.
- [ ] Run `dotnet build`.
- [ ] Publish a Windows x64 folder build.
