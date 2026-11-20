<p align="center">
  <img src="docs/assets/header-en.svg" width="100%" alt="Hold to Peek: hold to see your reference, release to keep working. Interaction concept illustration." />
</p>

<p align="center"><a href="README.md">简体中文</a> · <b>English</b></p>

<p align="center"><b>Keep your reference close, even with just one screen.</b><br />Hold to peek. Release to work. Pick up at the same line next time.</p>

<p align="center">
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0"><img src="https://img.shields.io/badge/Release-v2.0.0-2563eb?style=flat-square" alt="Release: v2.0.0" /></a>
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0"><img src="https://img.shields.io/badge/Platform-Windows%20x64-0078d4?style=flat-square" alt="Platform: Windows x64" /></a>
  <a href="docs/UBUNTU.md"><img src="https://img.shields.io/badge/Ubuntu-X11%20experimental-e95420?style=flat-square" alt="Ubuntu X11/Xorg experimental" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-22c55e?style=flat-square" alt="License: MIT" /></a>
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0"><img src="https://img.shields.io/badge/Portable-Runtime%20included-7c3aed?style=flat-square" alt="Portable: Runtime included" /></a>
</p>

<p align="center">
  <a href="#the-few-lines-you-need-right-by-your-cursor">Use cases</a> ·
  <a href="#four-actions-then-back-to-work">How it works</a> ·
  <a href="https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek-2.0.0-win-x64.zip">Download v2.0.0</a> ·
  <a href="https://github.com/JackieYangon/hold-to-peek/issues">Feedback</a>
</p>

> These captioned GIFs are interaction illustrations, not recordings. Some settings screens reflect 1.0.0; version 2.0.0 has separate shortcut and guide pages.

![Hold a side button to show the reference, release to hide it, and hold again to resume](docs/assets/demos/hold-en.gif)

[Download Ubuntu X11 experimental](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek_2.0.0_all.deb) · **[Download the portable Windows x64 app](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek-2.0.0-win-x64.zip)** · [Read the v2.0.0 release notes](https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0)

## What is new in 2.0.0

- **Capture with F1.** Skip the trip to the tray menu.
- **Choose your shortcuts.** Record keyboard chords for capture and peek, or use a middle/side mouse button for peek.
- **Find settings easily.** Reference, shortcuts, startup, and the guide have their own pages.
- **Opt in when useful.** Login startup and higher CPU priority are disabled by default.
- **Try it on Ubuntu.** An X11/Xorg experimental package is available; Wayland is not supported yet.

## The few lines you need, right by your cursor

You read five steps in a tutorial, switch to Settings, and immediately need to check step two again. Back to the browser, then back to Settings. A small task turns into a lot of window switching.

Hold to Peek is designed to bring that reference closer: **hold a mouse side button to see it beside your cursor; release to keep working.**

| What you are doing | What you want close by |
| --- | --- |
| Following a Windows settings tutorial | A short list of instructions |
| Filling out a form from a chat message | Names, addresses, and requirements |
| Entering shipping or order details | The small section you keep checking |
| Adjusting application settings | Option names and their values |

Made for the moments when you have one laptop screen and need to read while you work.

## Four actions, then back to work

Get started in four actions:

1. **Capture.** Close settings and press F1, or choose “框选参考” (Capture reference) from the tray, then select the area you need.
2. **Hold.** Return to your work and hold the trigger. Your reference appears beside the cursor without taking focus from the active window.
3. **Scroll.** Move the highlight to mark your place. The window underneath stays still.
4. **Release.** The reference disappears. Hold again to return to the same line.

**One screenshot. One highlight. One button within reach.**

| Action | What it does |
| --- | --- |
| Back mouse button, X1 | The default hold-to-peek trigger |
| F1 | Default capture shortcut; customizable |
| Custom key or chord | Click to record in Shortcuts; peek also supports middle and side mouse buttons |
| Mouse wheel | Move the highlight by the configured line spacing |
| Shift + mouse wheel | Fine adjustment: one original-image pixel per notch |
| Esc | Cancel a capture or dismiss the current peek |
| Tray menu → “暂停” (Pause) | Restore the trigger button's normal function |

The highlight marks your reading position. It does not recognize text or identify the next instruction automatically. Capturing a new reference resets the position.

**On Windows, adjust spacing with your reference in view.** Reference settings lets you try the current screenshot and see the highlight change as you move the slider, without losing your reading position. If you start a capture from settings, Esc returns you there and keeps the existing reference.

## See it once, then try it

### Capture only what you need

Press F1 or start a capture from the tray, drag around your reference, and release. Esc or right-click cancels while keeping the previous reference.

![Start from the tray, drag to select the reference area, and release to finish](docs/assets/demos/capture-en.gif)

### Keep your place as you read

Scroll while holding the trigger to move the highlight. Hold Shift as well to fine-tune its position by one original-image pixel per notch.

![Move the highlight with the wheel and fine-tune with Shift and the wheel](docs/assets/demos/wheel-en.gif)

### Adjust spacing without guessing

On Windows, move the slider in Reference settings while looking at the current screenshot, then try a few lines. The preview keeps your original reading position intact. Save when it feels right.

![Adjust the spacing slider, see the highlight change, and try the next line](docs/assets/demos/spacing-en.gif)

### No side button? Use F8

Open “按键设置” (Shortcuts), click the peek binding, press F8 or your preferred chord, and save. Hold to view and release to hide, just like a mouse side button.

![Choose F8 as the trigger, hold it to show the reference, and release to hide](docs/assets/demos/keyboard-en.gif)

### Get your button back whenever you need it

Pause from the tray when you need the side button's normal forward or back action. Resume to return to the same reference and reading position.

![Pause from the tray to restore normal button behavior, then resume peeking](docs/assets/demos/pause-en.gif)

## Get Hold to Peek

Current version: **2.0.0**. Choose your platform:

| Platform | Download and run |
| --- | --- |
| Windows x64 | [Download the portable ZIP](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek-2.0.0-win-x64.zip), extract, and run `HoldToPeek.exe`. The .NET runtime is included; no SDK needed. |
| Ubuntu 20.04+ · X11/Xorg experimental | [Download the .deb](https://github.com/JackieYangon/hold-to-peek/releases/download/v2.0.0/hold-to-peek_2.0.0_all.deb). See the [Ubuntu guide](docs/UBUNTU.md) for installation. **Wayland is unsupported; real Ubuntu desktop validation is pending.** |

Confirm your bindings in Shortcuts, save, then press F1 to capture. Closing settings leaves the app in the tray; choose Exit from the tray to quit.

[Release notes and SHA256 checksums](https://github.com/JackieYangon/hold-to-peek/releases/tag/v2.0.0) · [Changelog](docs/CHANGELOG.md)

If this is the tool you have been looking for, leave a **Star** or tell us in [Issues](https://github.com/JackieYangon/hold-to-peek/issues) which two windows you keep switching between.

## Before you use it

- **Your screenshot stays in local memory.** It is not uploaded or automatically saved to disk. The current screenshot and reading position remain available during the session and are cleared when you exit.
- **No mouse side buttons?** Record F8 or another keyboard shortcut: hold to view, release to hide.
- **Ubuntu requires X11/Xorg.** Keyboard and pointer input is temporarily grabbed during a peek and restored on release. Wayland is not supported.
- **Windows target: Windows 11 x64, regular desktop apps, and a single screen.** Windows 10, ARM64, multiple monitors, elevated windows, and games still need validation. Secure desktops and exclusive fullscreen apps are not guaranteed to work.
- **Windows uses a Simplified Chinese interface; Ubuntu provides bilingual labels.** This guide includes the relevant menu names.

[Development notes (Chinese)](docs/BUILD.md)

## License

This project is licensed under the [MIT License](LICENSE).
