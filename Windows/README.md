# Space7 Windows Edition (Preview)

Native Windows companion for the [Space7 macOS fork](https://github.com/dahlah77/space7). This is a separate C# Windows Forms implementation because original Swift/SwiftUI interfaces cannot run directly on Boot Camp Windows.

## Status

Implemented and tested on Apple MacBookPro16,2 running Windows 10 Pro x64 through Boot Camp:

- CPU/RAM/disk/uptime dashboard
- Process inspection and confirmed termination (core system processes protected)
- Manual user-Temp cleanup (never automatic or preselected)
- Installed-app inventory and Windows Apps & Features shortcut
- Window listing, focus and minimize
- Manual clipboard viewing/copying/clearing
- Screenshot into Pictures/Space7
- Shortcuts for Boot Camp Control Panel, Macs Fan Control, Device Manager, Task Manager, File Explorer, Sound Mixer and Windows Settings

Not yet ported: native macOS-specific helpers, per-app audio mixer, automatic clipboard history, advanced screen recording, and direct SMC fan-speed controls. Mac fan-speed control is intentionally delegated to Macs Fan Control when present.

## Build and install (Windows x64)

Requires Windows 10/11 and .NET Framework 4.x with its C# compiler at Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe. Boot Camp is optional for general features; hardware-specific tools appear only when installed.

In PowerShell:

    powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install

Creates Space7.exe, installs into %LOCALAPPDATA%\Programs\Space7, adds Start Menu/Desktop shortcuts, and registers an uninstaller for this user. No administrator permission is required.

To build without installing:

    powershell -ExecutionPolicy Bypass -File .\build.ps1

The app has a non-destructive diagnostics entry point: load its managed assembly and call Space7Windows.Program.SelfTest().

## Safety

Files are only removed from the current user's Temp directory when explicitly checked and confirmed; nothing is selected by default. Process termination and clipboard clearing likewise require deliberate actions. The app does not upload or persist system information.

## License

GPL-3.0-or-later, consistent with the upstream Space7/Vorssaint project. Not affiliated with, endorsed by or distributed by the original Vorssaint maintainers.
