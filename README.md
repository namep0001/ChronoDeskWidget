# ChronoDesk Widget

<sub><strong>Version 1.1.0 · Windows 10 / 11</strong></sub>

A lightweight Windows desktop companion built with C#, Windows Forms, and .NET Framework 4.8. ChronoDesk keeps productivity tools, calendars, market quotes, and system information accessible from one compact desktop widget.

**[Browse Windows releases](https://github.com/namep0001/ChronoDeskWidget/releases)** · **[Project website](https://namep0001.github.io/ChronoDeskWidget/)** · **[Source code](https://github.com/namep0001/ChronoDeskWidget)**

## Features

| Widget | Capabilities |
| --- | --- |
| Clock & Calendar | Live clock, Gregorian and Solar Hijri dates, and switchable calendar systems. |
| Tasks | Local task list with completion tracking. |
| Pomodoro | Focus and break timer with circular progress and dedicated controls. |
| Quick Notes | Locally saved notes with UTF-8 text export. |
| Market Rates | USD, EUR, and 18-karat gold per gram, with Toman or Dollar display and daily movement. |
| Network Monitor | Live download/upload speeds, internet status, monthly traffic totals, and daily usage. |
| System Monitor | CPU, RAM, GPU, system-drive usage, optional battery status, and uptime. |
| Calculator | Basic arithmetic, decimal input, and sign toggle. |

### Personalization
- English and Persian interface languages.
- Persian weekday labels and B Roya typography for the Solar Hijri calendar.
- System, Light, and Dark themes, including Windows accent color support.
- Adjustable opacity and font selection.
- Independent widget visibility, always-on-top mode, and optional Windows startup.
- System-tray controls.

## Download and install

1. Open the **[GitHub releases page](https://github.com/namep0001/ChronoDeskWidget/releases)**.
2. Download ChronoDeskWidget-Windows.zip.
3. Extract the archive.
4. Run ChronoDeskWidget.exe.

**Requirements:** Windows 10/11 and .NET Framework 4.8. No third-party runtime packages are required.

## Build from source

**Requirements**
- Windows 10 or Windows 11.
- .NET Framework 4.8.
- Windows .NET Framework C# compiler, or Visual Studio Build Tools with the .NET desktop build tools.

**Build and run**
1. Clone or download this repository.
2. Run build.bat to build the Release executable.
3. Run Start-Widget.bat to build when needed and launch the application.

The executable is generated at bin/Release/ChronoDeskWidget.exe. You can also open ChronoDeskWidget.csproj in Visual Studio and build the Release configuration.

## Repository layout

    ChronoDeskWidget/
    ├── .github/workflows/       # Windows CI and release automation
    ├── Source/
    │   ├── AssemblyInfo.cs     # Product and assembly metadata
    │   ├── Program.cs          # Application entry point
    │   ├── MainForm.cs         # Main window and widget composition
    │   ├── WidgetControls.cs   # Reusable controls and drawing
    │   ├── LocalStore.cs       # Local settings and data persistence
    │   ├── TgjuClient.cs       # Market quote retrieval and parsing
    │   ├── SystemWidgets.cs    # Network and system metrics
    │   └── Generate-AppIcon.ps1
    ├── ChronoDeskWidget.csproj
    ├── Start-Widget.bat
    ├── build.bat
    ├── CHANGELOG.md
    ├── index.html              # Project website
    └── README.md

## Data and privacy

Tasks, notes, settings, window position, network usage history, and startup error logs are stored locally under:

    %LOCALAPPDATA%\ChronoDeskWidget\

The application does not upload task, note, settings, or network-history data. Network monitoring reads Windows network counters and performs periodic connectivity checks. Usage history is collected only while the application is running; traffic during periods when it is closed cannot be reconstructed.

Market quotes are retrieved from public TGJU pages:
- [USD](https://www.tgju.org/profile/price_dollar_rl)
- [EUR](https://www.tgju.org/profile/price_eur)
- [18-karat gold](https://www.tgju.org/profile/geram18)

TGJU quotes are sourced in Rial. Toman display divides the source value by ten. Dollar display uses the live USD quote as its conversion reference. Market rates require an internet connection; unavailable source data is shown as unavailable.

## Build and release automation

- Pushes to main and pull requests targeting main run the Windows build workflow.
- Version tags matching v* trigger the Windows release workflow.
- The release workflow builds the application and packages ChronoDeskWidget.exe into a Windows ZIP archive.

## Version

- Product version: **1.1.0**
- Assembly version: **1.1.0.0**
- Target framework: **.NET Framework 4.8**

See [CHANGELOG.md](CHANGELOG.md) for release history.
