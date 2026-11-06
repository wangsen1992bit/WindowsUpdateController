# WindowsUpdateController

A tiny Windows Forms utility that pauses Windows updates the *soft-disable* way: it only writes a handful of registry values, never disables services or scheduled tasks, and everything can be undone with one click.

[中文说明 / Chinese documentation](README.zh-CN.md)

## Features

- **Pause updates** for 1–3650 days (quality and feature updates together)
- **Raise the pause ceiling** (`FlightSettingsMaxPauseDays`) so Windows does not truncate the pause to its default 5 weeks
- **Pin the current feature-update version** (`TargetReleaseVersion`) to block major upgrades while still receiving monthly security patches
- **One-click restore** — clears every value the tool wrote
- **Live status card** — paused / expired / not paused, deadline, remaining days, pinned version
- **Operation log** and a shortcut button that opens *Settings → Windows Update*

## Requirements

| Item | Detail |
| --- | --- |
| OS | Windows 10 / 11 (verified on Windows 11 25H2) |
| Permissions | Administrator — the embedded manifest requests elevation automatically |
| Runtime | .NET Framework 4.x (ships with Windows, nothing to install) |
| Build tools | none extra — uses the C# compiler bundled with Windows |

## Build

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "src\build.ps1"
```

Output: `dist\WindowsUpdateController.exe` (about 17 KB).

The script picks `csc.exe` from `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`, embeds `src\app.manifest` (`requireAdministrator` + DPI aware) and compiles all `src\*.cs` files. No NuGet, no SDK, no project file.

## Usage

1. Double-click `dist\WindowsUpdateController.exe` and accept the UAC prompt.
2. Pick a pause length (default **365** days) and leave **Pin current version** checked if you do not want major upgrades.
3. Click **Pause and pin version**.
4. Confirm in *Settings → Windows Update*; the page should show the updates as paused.
5. To undo everything, click **Restore updates** and then *Check for updates*.

## Registry keys touched

All writes are under `HKLM` and require elevation.

**Pause — `HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings`**

| Value | Type | Written |
| --- | --- | --- |
| `PauseFeatureUpdatesStartTime` | REG_SZ | now (ISO 8601 UTC) |
| `PauseQualityUpdatesStartTime` | REG_SZ | now |
| `PauseFeatureUpdatesEndTime` | REG_SZ | now + N days |
| `PauseQualityUpdatesEndTime` | REG_SZ | now + N days |
| `PauseUpdatesExpiryTime` | REG_SZ | now + N days |
| `FlightSettingsMaxPauseDays` | REG_DWORD | N + 30 |

**Version pin — `HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate`**

| Value | Type | Written |
| --- | --- | --- |
| `TargetReleaseVersion` | REG_DWORD | `1` |
| `TargetReleaseVersionInfo` | REG_SZ | current `DisplayVersion`, e.g. `25H2` |

*Restore* deletes all of the above. Nothing else is modified — no services, no scheduled tasks, no Group Policy objects.

## Caveats

- A paused system receives **no monthly security patches**. Treat this as a short-to-medium term measure and weigh the risk.
- The pause ends by itself at the deadline; the status card then reports *expired*.
- This is a soft disable, not a permanent block. Windows may still install updates if the registry values are reset by a feature update or by servicing.
- `TargetReleaseVersion` pins the *feature update* version only; quality updates keep flowing.

## Project layout

```
WindowsUpdateController/
├─ src/
│  ├─ Program.cs          entry point + global exception handling
│  ├─ MainForm.cs         UI (built in code, no designer files)
│  ├─ UpdateManager.cs    all registry read/write logic
│  ├─ app.manifest        requireAdministrator, DPI aware
│  └─ build.ps1           compiles to ../dist
└─ dist/
   └─ WindowsUpdateController.exe
```
