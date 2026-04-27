# Whip Cursor

> A tiny Windows tray app for whipping your screen when your AI agents need motivation.

## Run

Download the latest release from:

[GitHub Releases](../../releases/latest)

Then:

1. Download `WhipCursor-Installer.zip`.
2. Extract the zip.
3. Run `Install Whip Cursor.cmd`.

The installer adds Whip Cursor to your user account, creates shortcuts, and starts the app.

The app will appear in the system tray as **Whip Cursor**.

## Controls

| Action                | What it does                            |
| --------------------- | --------------------------------------- |
| Left-click            | Plays the whip animation at your cursor |
| F8                    | Arms or disarms the effect              |
| Ctrl+Shift+Q          | Quits the app                           |
| Right-click tray icon | Opens arm/disarm and exit options       |

Your clicks still pass through to whatever is underneath.

## Developer Release

To create the release installer locally, run:

```text
Make Release Installer.cmd
```

To create a GitHub release, see [RELEASE.md](RELEASE.md).
