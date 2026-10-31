# ChronoDesk Widget

<sub><strong>First official release · v1.0</strong></sub>

A lightweight Windows desktop companion built with C#, Windows Forms, and .NET Framework 4.8. It keeps everyday tools accessible on the desktop, stores personal task and note data locally, and connects to the internet only to retrieve market quotes.

**[Download v1.0](https://github.com/namep0001/ChronoDeskWidget/releases/tag/v1.0)** · **[Project website](https://namep0001.github.io/ChronoDeskWidget/)** · **[Source code](https://github.com/namep0001/ChronoDeskWidget)**

## What's included

- **Clock and calendar** — live clock, Gregorian and Solar Hijri dates, and a calendar that switches between both systems.
- **Tasks** — local task list with completion tracking.
- **Pomodoro** — focus and rest timer with circular progress and dedicated controls.
- **Quick notes** — locally saved notes with UTF-8 text export.
- **Market rates** — USD, EUR, and 18-karat gold per gram, with Toman or Dollar display and daily percentage movement.
- **Network monitor** — live download/upload speed, monthly traffic totals, and daily download/upload history retained from the first through the last day of each month.
- **Calculator** — compact desktop calculator with arithmetic, sign toggle, and decimal input.
- **System monitor** — CPU, RAM, GPU, disk, optional laptop battery usage, and system uptime with progress bars.
- **Appearance** — System, Light, and Dark themes, Windows accent color, adjustable opacity, and font selection.
- **Widget controls** — independently show or hide widgets, keep the widget on top, and launch with Windows.
- **Bilingual interface** — English and Persian, switchable from the system-tray menu.

## Download and install

1. Open the **[v1.0 release page](https://github.com/namep0001/ChronoDeskWidget/releases/tag/v1.0)**.
2. Download the Windows release package.
3. Extract the archive to a folder of your choice.
4. Run `ChronoDeskWidget.exe`.

Windows 10/11 and .NET Framework 4.8 are required. The app does not require third-party runtime packages.

## Run from source

**Requirements**
- Windows 10 or Windows 11
- .NET Framework 4.8
- Windows .NET Framework C# compiler, or Visual Studio Build Tools with the .NET desktop build tools

**Steps**
1. Clone or download this repository.
2. Run `Start-Widget.bat` to build when needed and launch the app.
3. Run `build.bat` to build without launching.

The executable is written to `bin/Release/ChronoDeskWidget.exe`. You can also open `ChronoDeskWidget.csproj` in Visual Studio and build the Release configuration.

## Repository layout

```text
ChronoDeskWidget/
├── .github/workflows/
│   ├── build-windows.yml       # Continuous Windows build
│   └── publish-release.yml     # Build and publish tagged releases
├── Source/
│   ├── AssemblyInfo.cs         # Product and assembly version metadata
│   ├── Program.cs              # Application entry point
│   ├── MainForm.cs             # Main window and widget composition
│   ├── WidgetControls.cs       # Reusable custom controls and drawing
│   ├── LocalStore.cs           # Local settings and data persistence
│   ├── TgjuClient.cs           # Market-rate retrieval and parsing
│   ├── SystemWidgets.cs        # Network usage and system metrics
│   └── Generate-AppIcon.ps1    # Generates the executable icon
├── ChronoDeskWidget.csproj
├── Start-Widget.bat
├── build.bat
├── CHANGELOG.md
├── index.html                  # Project landing page
└── README.md
```

## Data and privacy

Tasks, notes, settings, window position, network traffic history, and the startup error log are stored locally in:

```text
%LOCALAPPDATA%\ChronoDeskWidget\
```

Task, note, and network history data stay on the computer. Network history is sampled while the app is running, at most once per day and when Windows reports a network change; traffic while the app is closed cannot be reconstructed. Market-rate requests use public TGJU pages:

- [Dollar](https://www.tgju.org/profile/price_dollar_rl)
- [Euro](https://www.tgju.org/profile/price_eur)
- [18-karat gold](https://www.tgju.org/profile/geram18)

TGJU quotes are sourced in Rial. Toman display divides the source value by ten. Dollar display uses the live USD quote as its conversion reference. Internet access is required for market rates; unavailable source data is displayed as unavailable.

## Build and release automation

Every push to `main` and every pull request targeting `main` runs the Windows build workflow. Pushing a version tag such as `v1.0` runs the release workflow, which builds the executable and publishes a GitHub release containing the Windows ZIP package.

## Version

- Product version: **1.0**
- Assembly version: **1.0.0.0**
- Target framework: **.NET Framework 4.8**

See [CHANGELOG.md](CHANGELOG.md) for the v1.0 release notes.
