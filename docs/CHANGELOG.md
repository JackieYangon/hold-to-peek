# 更新记录 / Changelog

## 2.0.0

- 新增 Ubuntu 20.04+ X11/Xorg 实现和 `.deb` 打包脚本：内存框选、按住查看、阅读位置、可录入按键、独立页面、托盘和自启。
- Add an Ubuntu 20.04+ X11/Xorg implementation and Debian packaging, with in-memory capture, hold-to-peek, reading progress, editable bindings, separate pages, tray controls, and login startup.
- Wayland 暂不支持，启动时明确提示；Linux 负 nice 优先级受系统权限限制，不提权。Windows ZIP 与 Ubuntu 实验 .deb 同时提供于 2.0.0 Release，真实 Ubuntu 桌面验收待完成。
- Wayland is not supported yet. Linux priority changes respect OS permissions. The 2.0.0 Release includes a Windows ZIP and an experimental Ubuntu .deb; Ubuntu desktop validation remains pending.
- Ubuntu 的按住期间暂时接管键盘和鼠标来隔离滚轮，松手恢复。详见 [Ubuntu 使用与兼容说明](UBUNTU.md)。

## 1.5.0

- 新增启动与运行页：可选登录自启、静默托盘启动和可恢复的 CPU High 优先级。默认均关闭，失败会提示。
- Add opt-in login startup, quiet tray launch, and reversible High CPU scheduling priority with failure feedback.

## 1.4.0

- 按键设置移至独立页面，参考页专注于行距与试读；切页取消未完成录入。
- Give shortcut settings their own page and cancel unfinished recording when navigating away.

## 1.3.0

- 使用说明移至独立页面，随时可查看框选、阅读、暂停与隐私说明。
- Move the usage guide to its own always-accessible page.

## 1.2.0

- 框选与按住查看支持自定义键盘组合；查看支持鼠标中键与侧键，旧偏好自动兼容。
- Record custom keyboard chords for capture and peek, or middle/side mouse buttons for peek. Existing preferences remain compatible.
- 拒绝冲突和保留按键，编辑时暂停全局输入；源码更新，下载包仍为 1.0.0。

## 1.1.0

- 新增全局 F1 快速框选；长按不会重复开始。快捷键冲突时提示并保留托盘入口。
- Add global F1 capture with repeat suppression and conflict feedback; tray capture remains available.
- 源码更新，尚未发布新的下载包。Source update; no new release package yet.
