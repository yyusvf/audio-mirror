# Audio Mirror

Play Windows audio on **several output devices at once** — speakers *and* Bluetooth headphones
*and* a USB headset. No drivers, no virtual cables.

## Download

**[Get the latest release →](../../releases/latest)**

| File | |
|---|---|
| `AudioMirror-Setup.exe` | Installer. Start menu entry, optional desktop shortcut, normal uninstall. Detects x64 / ARM64 by itself. |
| `AudioMirror.exe` | Portable, single file (x64) |
| `AudioMirror-arm64.exe` | Portable, single file (Windows on ARM) |

Windows shows "Windows protected your PC" on first launch because the files are not
code-signed — click *More info → Run anyway*.

Needs **Windows 11** (or Windows 10 build 20348+). The interface is English, and German when
Windows is set to German.

## How it works

Tick a device — mirroring starts immediately. Untick it — it stops. There is no start button.

Expand a device with the arrow to see **every app currently playing sound**, each with its own
checkbox and volume slider. Leave them all on and the full device audio is mirrored, system sounds
included. Uncheck or lower one and only the selected apps are captured and mixed.

Device volume and app volume multiply: 50 % × 50 % = 25 %.

The window closes to the notification area; **Exit** in the tray menu quits for real. Your
selection is remembered per device and restored automatically, also after replugging.

## Latency

Measured on a USB DAC → HDMI monitor, 20 s per setting, no dropouts:

| Setting | Added latency (min / avg / max) |
|---|---|
| 20 ms | 16 / 24 / 40 ms |
| **30 ms** (default) | **24 / 32 / 45 ms** |
| 50 ms | 41 / 51 / 62 ms |

Bluetooth headphones add 100–200 ms of their own that no software can remove.

## Streaming

Audio Mirror outputs sound like any other program, so a recorder may pick it up **in addition** to
the original — measured:

- **Capturing a device** (OBS "Desktop Audio"): the mirror goes to a *different* device and does
  not show up there. Nothing to worry about.
- **Capturing "all applications"** (Discord screen share with desktop audio): Audio Mirror is
  captured too, so audio arrives twice and voice chat echoes back.

Two rules: capture a device or a specific app rather than "all applications", and untick voice
apps (Discord, TeamSpeak) for the mirrored device.

## Building

Needs the .NET 8 SDK.

```bash
dotnet publish AudioMirror.csproj -c Release -o dist
dotnet publish AudioMirror.csproj -c Release -r win-arm64 --self-contained true -o dist/arm64
ISCC.exe setup/AudioMirror.iss     # installer, needs Inno Setup
```

## Not yet tested

Several target devices at once, the ARM64 build, real Bluetooth targets, and physically
unplugging a device — no matching hardware available. Reports welcome.

## Licence

MIT — see [LICENSE](LICENSE).
