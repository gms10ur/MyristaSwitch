# MyristaSwitch

![MyristaSwitch logo](assets/logo.svg)

MyristaSwitch is a small Windows tray app for people who use a USB KMS switch
with a monitor that is still connected directly to each computer.

The app watches selected USB keyboard/mouse devices. When those devices appear
or disappear on the current PC, it applies a Windows display profile such as
`Extend` or `Internal only`. The goal is to combine the USB KMS button with the
monitor's normal signal detection, so switching machines does not also require
opening the monitor OSD and changing the input manually.

## Features

- Windows Forms tray app on .NET 9.
- Select the keyboard and mouse/HID devices that move with the KMS switch.
- Apply a display action when selected devices are connected.
- Apply another display action when selected devices are disconnected.
- Emergency restore hotkey: `Ctrl+Alt+Shift+M` forces `Extend` and pauses
  automation.
- Conservative single-display safeguard: actions that can leave a single-screen
  desktop without a visible display are skipped.
- Start with Windows and start minimized options.
- Test buttons for validating display actions before enabling automation.
- Filterable USB device lists.
- Auto detect mode that selects the KMS keyboard and mouse after they disappear
  from the current PC.
- LAN coordination between MyristaSwitch instances for KVM/KMS switches that
  keep keyboard and mouse devices emulated on inactive PCs.

## Recommended Setup

For a work laptop with its own built-in screen:

- Connected action: `Extend`
- Disconnected action: `InternalOnly`

This lets the laptop use the desk monitor while the KMS devices are connected,
then fall back to the laptop panel when the KMS is switched away.

For a desktop with only one connected monitor, the app currently avoids disabling
that display. Use `DoNothing` for the disconnected action on desktop machines.
A future version may add DDC/CI input switching for monitors that support it,
which would be a better fit for single-monitor desktop setups.

For KVM/KMS switches that keep USB devices visible on the inactive machine,
install and run MyristaSwitch on both PCs. When one PC detects that the switch
moved to it, it broadcasts that state on the local network and the other PC
applies its disconnected action.

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run --project .\MyristaSwitch.App\MyristaSwitch.App.csproj
```

## Publish and Package

Portable framework-dependent build:

```powershell
dotnet publish .\MyristaSwitch.App\MyristaSwitch.App.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  --output .\artifacts\MyristaSwitch-win-x64-portable
```

Full local release build with installer:

```powershell
.\tools\build-release.ps1
```

The installer uses Inno Setup. If `iscc.exe` is not available locally, install it
with:

```powershell
winget install --id JRSoftware.InnoSetup -e
```

The release workflow also installs Inno Setup and uploads both artifacts:

- Portable app: `MyristaSwitch-win-x64-portable`
- Installer: `MyristaSwitch-Setup-1.0.0-win-x64.exe`

See [docs/SETUP.md](docs/SETUP.md) for recommended laptop and desktop profiles.

## Safety Notes

Display switching can be disruptive if a profile is wrong for your hardware.
MyristaSwitch starts with automation disabled, stores settings in
`%APPDATA%\MyristaSwitch\settings.json`, and provides the global emergency hotkey
`Ctrl+Alt+Shift+M`.

## Roadmap

- DDC/CI monitor input switching for supported monitors.
- Per-machine presets for laptop and desktop workflows.
- A setup wizard that recommends safe defaults after scanning displays.

## License

MIT
