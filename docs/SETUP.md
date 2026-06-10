# Setup Guide

## Laptop profile

Use this when the laptop has its own built-in panel and the desk monitor is the
second display.

- Keyboard: select the keyboard device that moves through the KMS switch.
- Mouse / HID: select the mouse or receiver that moves through the KMS switch.
- When KMS is here: `Extend`
- When KMS leaves: `InternalOnly`
- Safety: keep `Require both devices` enabled unless your KMS exposes only one
  stable HID device.

## Auto detect

Auto detect is the easiest way to pick the right USB devices when Windows shows
many similar keyboard, mouse, and HID entries.

1. Make sure the KMS is currently switched to this PC, so the keyboard and mouse
   are working on it.
2. Open MyristaSwitch.
3. Click `Auto detect`.
4. Press the physical KMS switch button once, moving the keyboard and mouse away
   from this PC.
5. Wait a few seconds. MyristaSwitch compares the before and after device list
   and selects the keyboard and mouse/HID devices that disappeared.
6. Review the selected devices, then choose the connected and disconnected
   display actions.
7. Click `Save settings`.
8. Enable automation when the selected actions are correct for the machine.

If only one device disappears, MyristaSwitch selects it and disables `Require
both devices`. Review the selected devices manually before enabling automation.

MyristaSwitch listens for Windows USB device-change notifications and also polls
once per second while automation is enabled, so KMS connect/disconnect changes
should be picked up shortly after the physical switch is pressed.

## Filtering device lists

Use the `Filter` boxes above the keyboard and mouse selectors to narrow long USB
device lists. The filter searches both the friendly device name and the Windows
instance ID.

## Desktop profile

For a desktop with only one monitor, keep the disconnected action at `DoNothing`.
Windows display profiles are not a good way to switch a single desktop monitor
away from that PC because disabling the only visible display can leave the
machine hard to recover.

For this setup, a future DDC/CI monitor input switcher is the right feature.

## Recovery

Press `Ctrl+Alt+Shift+M` at any time to force Windows back to `Extend` and pause
automation.

The app stores settings at:

```text
%APPDATA%\MyristaSwitch\settings.json
```
