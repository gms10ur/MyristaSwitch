# Changelog

## 1.0.5

- Added LAN broadcast coordination between MyristaSwitch instances.
- When one PC detects that the KMS switched to it, it announces that it is
  active; other PCs running MyristaSwitch apply their away action.
- This handles KVMs that keep keyboard/mouse devices emulated on inactive PCs,
  where local USB disconnect detection cannot reliably see `Here` to `Away`.

## 1.0.4

- Added Raw Input based device presence detection for KMS state changes.
- Uses Raw Input hardware tokens for active/inactive checks when available, so
  stale PnP device nodes should no longer block `Here` to `Away` transitions.
- Shows the Raw Input token count in the monitoring status line.

## 1.0.3

- Restored USB device listing to the reliable `Get-PnpDevice -PresentOnly`
  source after the richer presence query proved unreliable on some systems.
- Made the settings labels match the intended wording: `Enable`, `Require both
  mouse and keyboard`, and `Launch at login`.
- Changed the settings layout to a single column so long labels are not clipped.
- Auto-saves the main toggle settings when checkboxes change.

## 1.0.2

- Changed the main automation label to `Enable`.
- Renamed the strict matching option to `Require both mouse and keyboard`.
- Renamed startup launch to `Launch at login`.
- Improved KMS presence detection by using only usable devices for active/missing
  state checks.
- Added keyboard/mouse state details to the monitoring status line.

## 1.0.1

- Improved KMS device detection by reacting to Windows USB device-change events
  in addition to the background poll.
- Improved USB presence detection by checking device present/status fields
  instead of relying only on `Get-PnpDevice -PresentOnly`.
- Simplified the settings UI to focus on automation, sign-in launch, and
  keyboard-plus-mouse matching.
- Removed user-facing poll interval and start-minimized options.

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
