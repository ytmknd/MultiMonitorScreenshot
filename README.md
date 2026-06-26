# MultiMonitorScreenshot

A simple **Windows** desktop app for capturing screenshots across multiple monitors — capture an individual display, the primary display, or the entire multi-monitor virtual screen as a single image.

## Features

- **Visual monitor layout** — displays all connected monitors in their actual arrangement (gaps between displays are automatically collapsed, like the Windows Settings app).
- **Capture a single monitor** — click any monitor in the layout to capture just that display.
- **Capture the primary monitor** — one-click capture of the primary display.
- **Capture all monitors** — captures the full bounding rectangle of every display (the virtual screen) as a **single** combined image.
- **Quick access** — open the screenshot folder directly from the app.

- **Record MP4 video** - records a selected monitor or all monitors to H.264 MP4 through ffmpeg.

## Requirements

- Windows (x64)
- [ffmpeg](https://ffmpeg.org/) - install it on PATH, or place `ffmpeg.exe` next to `MultiMonitorScreenshot.exe`. If `ffmpeg.exe` is placed in the project folder, it is copied to build and publish output automatically.
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — *not required if you use the self-contained build or the installer, which bundle the runtime.*

## Where screenshots are saved

Screenshots are saved as PNG files and recordings are saved as MP4 files to:

```
%USERPROFILE%\Pictures\Screenshots\
```

The `Pictures` folder is resolved through the Windows known folder API, so its display name may vary by Windows language settings. The app-created subfolder name is always `Screenshots`.

with timestamped filenames such as `Screenshot_20260624_153012_487.png` (millisecond precision avoids overwriting when capturing in quick succession).
Recordings use names such as `Recording_20260624_153012.mp4`.

## Building from source

```sh
dotnet build MultiMonitorScreenshot/MultiMonitorScreenshot.csproj -c Release
```

### Publishing a self-contained, single-file build

```sh
dotnet publish MultiMonitorScreenshot/MultiMonitorScreenshot.csproj -c Release -p:PublishProfile=FolderProfile
```

Output is written to:

```
MultiMonitorScreenshot/bin/Release/net8.0-windows/publish/win-x64/
```

This build is self-contained (the .NET runtime is bundled), so it runs without installing .NET separately.

## Building the installer (optional)

An [Inno Setup](https://jrsoftware.org/isinfo.php) script is provided.

1. Run the self-contained publish step above.
2. If you want the installer to include ffmpeg, place a Windows x64 `ffmpeg.exe` at:

   ```
   MultiMonitorScreenshot/ffmpeg.exe
   ```

   The installer script copies that file into the application folder when it exists. The app looks for `ffmpeg.exe` next to `MultiMonitorScreenshot.exe` first, then falls back to `ffmpeg` on `PATH`.

   If this file is not present when the installer is built, the installer is still created, but MP4 recording will only work on PCs where ffmpeg is already available on `PATH`.

3. Compile the installer:

   ```sh
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\MultiMonitorScreenshot.iss
   ```

4. The setup executable is generated in `installer\Output\`.

### ffmpeg packaging notes

- `ffmpeg.exe` is not downloaded by the build or installer script. Download it separately from the ffmpeg project or another trusted distributor before building the installer.
- For a redistributable installer, prefer bundling `ffmpeg.exe` in the app folder instead of relying on the user's system `PATH`.
- Check the license terms of the ffmpeg build you redistribute. ffmpeg builds may be GPL or LGPL depending on how they were compiled and which codecs are enabled.
- The current recorder writes H.264 MP4 through ffmpeg using `libx264`, so use a build that includes that encoder.

## Tech stack

- .NET 8 / Windows Forms
- Target: `win-x64`

## License

This project is licensed under the [MIT License](LICENSE).
