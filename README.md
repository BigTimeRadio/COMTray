# COM Port Tray

A small Windows system tray tool that watches for serial (COM) ports appearing and
disappearing. When a new port shows up it pops a notification such as

> COM7 is the new port for USB-K4Y Radio Cable, plugged into Left USB Port.

The tray icon always shows the number of the most recent port.

## Behaviour

- **Event driven.** It listens for `WM_DEVICECHANGE` from Windows and rescans only when
  the device tree actually changes. There is no background polling, so it sits idle at
  ~0% CPU until something is plugged in or removed.
- **Quiet on launch.** Every port present at startup is recorded silently, so ports that
  already exist on boot (Bluetooth links, onboard serial, anything left plugged in) never
  trigger a notification. Only ports that arrive after launch are announced.
- **Names belong to the USB socket, not the device.** The tool walks the device tree to
  the USB device behind each COM port and identifies the physical socket by its topology
  path (stable across reboots and through external hubs). You name the socket, e.g. "Left
  USB C". Whatever cable you later plug into that socket reports the same name, while the
  device description ("USB-K4Y Radio Cable") tells you what is currently in it. This is
  deliberate: many chipsets (for example a default CP2102) share an identical USB identity,
  so the cable cannot be told apart, but the socket always can. Names are stored in
  `%APPDATA%\ComTray\names.json`.
- **Non-USB ports** (onboard serial, Bluetooth virtual ports) are still detected and
  announced; they just have no USB socket to name.

## Tray menu

- The latest COM port number
- Each current port, showing its device and USB socket name, with a "Name this USB port"
  option (USB ports only)
- **Start with Windows** to toggle launch at sign in
- **Exit**

## Build

Requires the .NET 10 SDK.

```powershell
.\build.ps1
```

This produces a standalone `dist\ComTray.exe` with the runtime bundled in, so it runs on
any 64-bit Windows machine with nothing else installed. For development, `dotnet run
--project src\ComTray` works too.

## Run at startup

Use **Start with Windows** in the tray menu. It registers the exe under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Keep `ComTray.exe` somewhere stable
(for example `%LOCALAPPDATA%\ComTray`) before enabling it, since the registry entry points
at the exe's current location.
