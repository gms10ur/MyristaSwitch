# Changelog

## 1.0.0

- Added a Windows tray app for KMS-aware display profile switching.
- Added keyboard and mouse/HID device selection.
- Added connected and disconnected display actions.
- Added emergency restore hotkey: `Ctrl+Alt+Shift+M`.
- Added conservative single-display safeguards.
- Added start-with-Windows and start-minimized options.
- Added test buttons for display actions.
- Added logo, MIT license, release documentation, GitHub Actions build, and
  Windows installer packaging.
- Added immediate USB device-change polling so KMS connect/disconnect changes
  are detected without waiting only for the timer.
- Simplified automation/startup settings in the main UI.
