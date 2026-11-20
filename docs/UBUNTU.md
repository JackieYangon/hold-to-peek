# Ubuntu 版 / Ubuntu edition

## 中文

Ubuntu 20.04 及以上的 **X11/Xorg 桌面会话**可使用此平台实现。采用 Python 3.8+、GTK 3 与 X11，不需要 .NET。当前是 2.0.0 源码与本地实验包，尚未发布 Linux Release，也尚未完成 Ubuntu 实机验收。

**Wayland 暂不支持。** 程序会识别并提示，不会冒充完整支持。若登录界面有“Ubuntu on Xorg”，可注销后选择它；没有 Xorg 会话的系统需要另行准备兼容桌面。不要仅通过设置 `GDK_BACKEND=x11` 使用 XWayland，它无法控制原生 Wayland 应用。

### 安装

在仓库根目录构建本地包（Windows 或 Linux 均可运行 Python 打包脚本）：

```sh
python3 scripts/package-linux.py
```

把生成的 `.deb` 放到 Ubuntu，然后安装：

```sh
sudo apt install ./hold-to-peek_2.0.0_all.deb
hold-to-peek
```

`apt` 会从系统源安装 GTK、Python Xlib 和托盘依赖。程序以普通用户运行。安装包为 `all` 架构，依赖由系统提供；实际 CPU 架构与桌面组合仍需验证。

从源码运行需安装相同依赖（20.04 的托盘包可能为 `gir1.2-appindicator3-0.1`）：

```sh
sudo apt install python3 python3-gi python3-gi-cairo python3-xlib gir1.2-gtk-3.0 gir1.2-ayatanaappindicator3-0.1
python3 src/HoldToPeek.Linux/hold-to-peek
```

### 使用

- 默认 F1 框选，按住鼠标后退侧键查看；也可在独立按键页录入键盘组合键、中键或侧键。
- 滚轮移动高亮，Shift + 滚轮精调，松开隐藏，再按保留位置。当前按住查看期间临时接管键盘和鼠标，输入不会发给底层窗口；松手后恢复。原工作窗口焦点保留。
- 框选时 Esc 或右键取消，保留旧图。显示器变更、锁屏与休眠会收起临时界面。
- 从托盘打开设置、暂停/恢复、清除参考或退出。没有托盘组件时，关闭设置会最小化窗口，仍可从任务栏返回。
- “登录后启动”在安装 `.deb` 后可用，默认关闭，启用时创建当前用户的 XDG 自启条目。
- CPU 优先级默认普通。Linux 可能拒绝负 nice 值；会提示并保留原优先级，不申请 root 权限。
- 截图只存在内存，退出清除。偏好位于 `${XDG_CONFIG_HOME:-~/.config}/hold-to-peek/settings.json`，自启条目位于相邻 `autostart/hold-to-peek.desktop`。

卸载：`sudo apt remove hold-to-peek`。卸载前可先关闭自启；用户偏好不会被系统包管理器删除。

### 已验证与待验证

Windows 开发机上检查 Python 3.8 语法兼容、核心逻辑、X11 事件状态模拟、包结构和校验值。未安装 Ubuntu/WSL，未执行实际 GTK 显示、X11 输入、DPI、锁屏/休眠或 `apt` 安装验收。不能把这些静态与模拟检查等同于桌面支持认证。

## English

This implementation targets **Ubuntu 20.04+ with an X11/Xorg desktop session**, using Python 3.8+, GTK 3, and X11. It does not require .NET. Version 2.0.0 is currently a source update and local experimental package, not a published Linux release or a desktop-validated build.

**Wayland is not supported yet.** The app reports this explicitly. Select an Xorg session at login if your system provides one; forcing XWayland cannot control native Wayland applications.

Build with `python3 scripts/package-linux.py`, copy the generated package to Ubuntu, and install with `sudo apt install ./hold-to-peek_2.0.0_all.deb`. Launch `hold-to-peek` from the terminal or application menu. Dependencies come from Ubuntu repositories; no root privileges are used to run the app.

F1 starts capture. Hold the back mouse button to peek; scroll to move the highlight, use Shift for fine adjustment, and release to hide. Both bindings are configurable. While peeking, X11 keyboard and pointer input is temporarily grabbed to isolate scrolling, then released. Escape or right-click cancels capture without discarding the previous reference.

Separate pages cover reference spacing, shortcuts, startup/CPU preferences, and usage. Login startup is opt-in after installing the package. Linux may deny higher CPU priority; this is reported without requesting administrator access. Screenshots stay in memory, and only preferences are stored in the XDG configuration directory.

Python compatibility, core logic, simulated X11 event handling, and package integrity are checked on the Windows development machine. Real GTK/X11 input, desktop focus, DPI, suspend/lock behavior, and apt installation remain unverified. Please report the Ubuntu release and `echo "$XDG_SESSION_TYPE"` with any feedback.
