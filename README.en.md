<p align="center">
  <img src="docs/assets/header-en.svg" width="100%" alt="Hold to Peek: hold to see your reference, release to keep working. Interaction concept illustration." />
</p>

<p align="center"><a href="README.md">简体中文</a> · <b>English</b></p>

<p align="center"><b>Keep your reference close, even with just one screen.</b><br />Hold to peek. Release to work. Pick up at the same line next time.</p>

<p align="center"><b>Windows</b> · <b>In development</b> · First release target: <b>1.0.0</b></p>

<p align="center">
  <a href="#the-few-lines-you-need-right-by-your-cursor">Use cases</a> ·
  <a href="#four-actions-then-back-to-work">How it works</a> ·
  <a href="#get-hold-to-peek">Get the app</a> ·
  <a href="https://github.com/JackieYangon/hold-to-peek/issues">Feedback</a>
</p>

> **The first release is in development. No verified download is available yet.** The banner is a concept illustration, not a screenshot of the running app.

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

The first release is built around this flow:

1. **Capture.** Choose “框选参考” (Capture reference) from the tray menu and select the area you need.
2. **Hold.** Return to your work and hold the trigger. Your reference appears beside the cursor without taking focus from the active window.
3. **Scroll.** Move the highlight to mark your place. The window underneath stays still.
4. **Release.** The reference disappears. Hold again to return to the same line.

**One screenshot. One highlight. One button within reach.**

| Action | What it does |
| --- | --- |
| Back mouse button, X1 | The default hold-to-peek trigger |
| Forward mouse button, X2 / keyboard F8 | Alternative triggers selectable in settings |
| Mouse wheel | Move the highlight by the configured line spacing |
| Shift + mouse wheel | Fine adjustment: one original-image pixel per notch |
| Esc | Cancel a capture or dismiss the current peek |
| Tray menu → “暂停” (Pause) | Restore the trigger button's normal function |

The highlight marks your reading position. It does not recognize text or identify the next instruction automatically. Capturing a new reference resets the position.

## Get Hold to Peek

Development and validation are underway. **There is no verified app or installer to download yet.** A download link will appear here when a usable release is ready.

If this is the tool you have been looking for, leave a **Star** or tell us in [Issues](https://github.com/JackieYangon/hold-to-peek/issues) which two windows you keep switching between.

## Before you use it

- **Your screenshot stays in local memory.** It is not uploaded or automatically saved to disk. The current screenshot and reading position remain available during the session and are cleared when you exit.
- **No mouse side buttons?** Select F8 in settings: hold to view, release to hide.
- **Initial target: Windows 11 x64, regular desktop apps, and a single screen.** Windows 10, ARM64, multiple monitors, elevated windows, and games still need validation. Secure desktops and exclusive fullscreen apps are not guaranteed to work.
- **The app interface is currently in Simplified Chinese.** This English guide includes the relevant menu labels.

[Development notes (Chinese)](docs/BUILD.md)
