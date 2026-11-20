# 本地开发与构建

## 环境

- Windows 11 x64 为当前首版验证目标。
- .NET 10 LTS SDK，`global.json` 固定 10.0.404 特性带，允许后续补丁。
- 本项目无第三方 NuGet 包；WPF、Windows Forms 和 Windows 原生接口由 .NET/Windows 提供。

可安装微软官方 SDK，或把 SDK 解压到仓库 `.dotnet/`。脚本优先使用项目 SDK，并把 CLI 状态及包缓存放到已忽略的 `.dotnet-home/`、`.nuget/`。不要把 SDK、缓存或下载文件加入 Git。

## 常用命令

从项目根目录运行：

```powershell
powershell -NoProfile -File scripts/build.ps1
powershell -NoProfile -File scripts/run.ps1
powershell -NoProfile -File scripts/publish.ps1
```

`build.ps1` 默认构建 Release；`run.ps1` 构建 Debug 后启动本机程序。发布脚本准备 Windows x64 自带运行时的包，输出到 `artifacts/hold-to-peek-1.0.0-win-x64/` 和对应 ZIP；实际目录版本来自 `VERSION`，不需要用户另装 SDK。发布时可能需要从 NuGet 获取官方运行时包。

发布前需完成对应版本的功能验收，并在发布说明中注明已验证环境与已知限制。

## 代码结构

| 目录或文件 | 职责 |
| --- | --- |
| `src/HoldToPeek/Program.cs` | 入口与单实例控制 |
| `src/HoldToPeek/PeekApplication.cs` | 托盘、生命周期、输入与窗口协作 |
| `src/HoldToPeek/Core/` | 阅读进度和触发类型 |
| `src/HoldToPeek/Input/` | 专用线程的全局监听、按住状态与恢复 |
| `src/HoldToPeek/Services/` | 截图、显示器坐标、用户偏好 |
| `src/HoldToPeek/Views/` | 设置、框选、非激活参考窗口 |
| `scripts/` | 正式构建、启动、发布脚本 |

截图与阅读位置只保存在内存；设置保存在 `%LOCALAPPDATA%\HoldToPeek\settings.json`。输入钩子仅将事件排队到 UI 线程，不进行文件写入、截图或绘制。预览窗口使用 `WS_EX_NOACTIVATE` 和鼠标穿透；框选窗口则正常获取焦点。

## 本地验证

先运行构建脚本，再通过启动脚本在 Windows 交互桌面验证：框选与取消、侧键/F8 按住与释放、滚轮隔离、阅读位置、暂停和退出。补充检查锁屏恢复及不同 DPI；截图取消应保留旧参考，设置窗口关闭不应退出托盘程序。

本地测试代码、夹具和测试结果统一放在被忽略的 `tests/`，不随仓库分发。输入与阅读逻辑、设置持久化可以独立验证；鼠标位置、焦点和截图相关检查需要交互式桌面，后台命令运行器不能替代这些验收。

## 版本和协作

`VERSION` 为版本值唯一来源，构建属性由 `Directory.Build.props` 派生。当前已交付的 `1.0.0` 是版本基线，对应源码提交 `738f3c3`。后续每批完整产品改动按 `a.b.c` 递增：大型功能更新或增加递增 `a`，小规模功能优化递增 `b`，问题修复递增 `c`；高位递增时低位归零，同批包含多类改动时取最高级别。下一版仅修复为 `1.0.1`，小规模优化为 `1.1.0`，大型更新为 `2.0.0`。

产品改动应同步更新 `VERSION`、应用版本和更新说明，再构建对应版本的分发包，保留已有 `1.0.0` 包。纯文档、计划或本地测试调整不自动递增产品版本。交付版本与 GitHub Release 分开记录，不将本地打包视为已公开发布。

设计与验收见 [开发计划](DEVELOPMENT_PLAN.md)。README 中英文两版只保留面向用户的信息，工程细节维护在本文件。
