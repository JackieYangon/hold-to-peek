<p align="center">
  <img src="docs/assets/header-zh.svg" width="100%" alt="瞄一眼 · Hold to Peek：按住看参考，松手接着干。交互概念示意。" />
</p>

<p align="center"><b>简体中文</b> · <a href="README.en.md">English</a></p>

<p align="center"><b>只有一块屏幕，也能把参考放在手边。</b><br />按住看一眼，松手接着干，再按还在刚才那一行。</p>

<p align="center">
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0"><img src="https://img.shields.io/badge/Release-v2.0.0-2563eb?style=flat-square" alt="已发布: v2.0.0" /></a>
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0"><img src="https://img.shields.io/badge/%E5%B9%B3%E5%8F%B0-Windows%20x64-0078d4?style=flat-square" alt="平台: Windows x64" /></a>
  <a href="docs/UBUNTU.md"><img src="https://img.shields.io/badge/Ubuntu-X11%20experimental-e95420?style=flat-square" alt="Ubuntu X11/Xorg experimental" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/%E5%8D%8F%E8%AE%AE-MIT-22c55e?style=flat-square" alt="协议: MIT" /></a>
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0"><img src="https://img.shields.io/badge/%E5%85%8D%E5%AE%89%E8%A3%85-%E8%87%AA%E5%B8%A6%E8%BF%90%E8%A1%8C%E6%97%B6-7c3aed?style=flat-square" alt="免安装: 自带运行时" /></a>
</p>

<p align="center">
  <a href="#需要的那几行就在鼠标旁">使用场景</a> ·
  <a href="#四个动作接着干">如何使用</a> ·
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek-2.0.0-win-x64.zip">下载 v2.0.0</a> ·
  <a href="https://github.com/JackieYangon/hold-to-peek/issues">反馈与建议</a>
</p>

> 下方为带字幕的交互示意，非实机录屏；部分设置界面展示基于 1.0.0，2.0.0 的按键和说明已独立成页。

![按住侧键显示参考，松开隐藏，再次按住保留阅读位置](docs/assets/demos/hold-zh.gif)

[下载 Ubuntu X11 实验版](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek_2.0.0_all.deb) · **[下载 Windows x64 免安装版](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek-2.0.0-win-x64.zip)** · [查看 v2.0.0 发布说明](https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0)

## 2.0.0，让操作更顺手

- **按 F1 直接框选。** 不用每次去找托盘菜单。
- **按键由你决定。** 框选和查看都支持键盘组合键；查看还可以用鼠标中键、侧键。
- **设置各有去处。** 参考、按键、启动与运行、使用说明分成独立页面。
- **需要时再开启。** 登录自启和 CPU 高优先级默认关闭，可在设置中启用。
- **Ubuntu 也有版本。** 提供 X11/Xorg 实验包；Wayland 暂不支持。

## 需要的那几行，就在鼠标旁

教程里的五个步骤刚看完，切到设置窗口，第二步就想再确认一下。切回网页，看完再回来——一件小事，要来回好几趟。

「瞄一眼」想让参考离你近一点：**按住侧键，内容浮在鼠标旁；松开，继续手头的事。**

| 你正在做什么 | 留在手边的参考 |
| --- | --- |
| 跟着教程改 Windows 设置 | 网页里的几条操作步骤 |
| 照着聊天内容填表 | 姓名、地址和填写要求 |
| 录入快递或订单信息 | 需要反复核对的一小块内容 |
| 对照说明调应用参数 | 选项名称和对应数值 |

尤其适合只有一块笔记本屏幕、又需要一边看一边操作的时候。

## 四个动作，接着干

开始使用只需要四个动作：

1. **框住。** 关闭设置后按 F1，或从托盘选择「框选参考」，留下需要看的那一块。
2. **按住。** 切回工作窗口，按住触发键，参考在鼠标旁出现，不抢走原窗口焦点。
3. **滚动。** 滚轮移动高亮线，标记读到的位置；底层页面不会跟着滚动。
4. **松手。** 参考收起，继续操作。再次按住，仍停在刚才那一行。

**一张截图，一条高亮线，一个顺手的触发键。**

| 操作 | 用途 |
| --- | --- |
| 鼠标后退侧键 X1 | 默认的按住触发键 |
| F1 | 默认快速框选键，可自行修改 |
| 自定义按键 / 组合键 | 在「按键设置」点击后录入；查看还支持中键和侧键 |
| 滚轮 | 按设置的行距移动高亮线 |
| Shift + 滚轮 | 微调高亮位置，每格移动 1 个原图像素 |
| Esc | 取消框选，或收起本次预览 |
| 托盘菜单「暂停」 | 暂停接管触发键，恢复按键原来的功能 |

高亮线标记的是阅读位置，不会自动识别文字或判断教程的“下一步骤”。换一张参考截图后，阅读位置重新开始。

**Windows 行距不合适？直接看着参考调。** 设置里可以用当前截图试读，拖动滑块就能看到高亮变化；试读不会打乱原来的阅读位置。从设置开始框选后，按 Esc 取消会回到设置，原参考仍保留。

## 看一遍，就会用

### 只截需要看的内容

按 F1 或从托盘开始框选，拖出参考区域，松开完成。想重来？按 Esc 或右键取消，旧参考仍然保留。

![从托盘开始框选，拖动选取参考内容，松开完成](docs/assets/demos/capture-zh.gif)

### 读到哪，就标到哪

按住查看时，滚轮移动高亮线。想对得更准，按住 Shift 再滚动，每格微调一个原图像素。

![滚轮移动高亮，Shift 加滚轮精细调整阅读位置](docs/assets/demos/wheel-zh.gif)

### 行距不用靠猜

Windows「参考设置」里可以看着当前参考拖动滑块，再试读几行。试读不会改动原来的阅读位置，满意后保存即可。

![调整行距滑块，观察高亮变化并试读下一行](docs/assets/demos/spacing-zh.gif)

### 没有侧键，也能用

在「按键设置」点击查看键按钮，按下 F8 或其他顺手的组合键，再保存。按住查看，松开收起，使用方式一样。

![把触发键改成 F8，按住显示参考，松开隐藏](docs/assets/demos/keyboard-zh.gif)

### 按键的原功能，随时还给你

需要用侧键前进或后退时，在托盘菜单暂停。恢复后，参考和阅读位置都还在。

![托盘暂停后恢复侧键原功能，再恢复瞄一眼继续查看](docs/assets/demos/pause-zh.gif)

## 获取瞄一眼

当前版本 **2.0.0**。按你的系统选择：

| 平台 | 下载与运行 |
| --- | --- |
| Windows x64 | [下载免安装 ZIP](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek-2.0.0-win-x64.zip)，解压后运行 `HoldToPeek.exe`；自带 .NET 运行时，无需安装 SDK |
| Ubuntu 20.04+ · X11/Xorg 实验版 | [下载 .deb](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek_2.0.0_all.deb)，安装方法与限制见 [Ubuntu 使用说明](docs/UBUNTU.md)；**不支持 Wayland，尚未完成实机验收** |

首次打开，在「按键设置」确认快捷键，保存后按 F1 框选。关闭设置后仍在托盘运行，右键托盘选择「退出」可彻底关闭。

[发布说明与 SHA256 校验文件](https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0) · [更新记录](docs/CHANGELOG.md)

如果这正是你需要的小工具，欢迎点一个 **Star**，或到 [Issues](https://github.com/JackieYangon/hold-to-peek/issues) 说说你最常在哪两个窗口之间来回切换。

## 使用前了解一下

- **截图留在本机内存中。** 不上传，不自动保存到磁盘；退出程序后清除。本次运行内保留当前截图和阅读位置。
- **没有侧键也能用。** 在「按键设置」录入 F8 等键盘按键，按住查看、松开隐藏。
- **Ubuntu 实验版需要 X11/Xorg。** 按住查看期间临时接管键盘和鼠标，松手恢复；Wayland 不支持。
- **Windows 首轮面向 Windows 11 x64、普通桌面应用和单屏场景。** Windows 10、ARM64、多屏、管理员窗口及游戏场景仍待验证；不承诺安全桌面或独占全屏兼容。

[开发说明](docs/BUILD.md)

## 开源协议

本项目采用 [MIT License](LICENSE)。
