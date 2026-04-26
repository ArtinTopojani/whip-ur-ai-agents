# Whip Cursor

Need to whip your AI agents back into line? I got you.

Whip Cursor is a tiny Windows tray app that whips at your mouse pointer whenever you left-click. The animation follows your cursor while it plays, so it feels like you are actually cracking the whip across the screen.

## Run it

Double-click `Run Whip Cursor.cmd`.

If you want to rebuild it manually, double-click `Build Whip Cursor.cmd`.

The app appears in the system tray as "Whip Cursor."

## Controls

- Left-click: play the whip video at the cursor. It follows the cursor while it plays.
- F8: arm or disarm the effect.
- Ctrl+Shift+Q: quit.
- You can also right-click the tray icon to arm/disarm or exit.

The click still passes through to whatever is underneath.

## How it works

The app runs quietly in the system tray and listens for left-clicks while it is armed. When you click, it opens a tiny transparent, click-through overlay window on top of your desktop.

That overlay renders the frames from `whip.mov` itself, including the transparent background, then moves with your cursor until the whip animation finishes. Because the overlay is click-through, your click still hits the app, window, or button underneath like normal.

No cloud stuff, no account, no drama. Just a local desktop whip for when the agents need a little motivation.
