# ChronoDesk Widget

A lightweight Windows desktop companion built with C#, Windows Forms, and .NET Framework 4.8. The application runs locally, keeps tasks and notes on the device, and retrieves online data only for market quotes.

**[Project website](https://namep0001.github.io/ChronoDeskWidget/)** · **[Source code](https://github.com/namep0001/ChronoDeskWidget)** · **[Latest Windows build](https://github.com/namep0001/ChronoDeskWidget/actions/workflows/build-windows.yml)**

## Features

- **Clock and calendar** — Gregorian and Solar Hijri dates, with a calendar that can switch between both systems.
- **Tasks** — local task list with completion tracking.
- **Pomodoro** — focus and rest timer with circular progress and timer controls.
- **Quick notes** — notes saved locally, with export to a UTF-8 text file.
- **Market rates** — USD, EUR, and 18-karat gold per gram, displayed in Dollar ($) or Toman (T).
- **Appearance** — System, Light, and Dark themes; Windows accent color; adjustable opacity and font.
- **Widget controls** — independently show or hide widgets, keep the widget on top, and start with Windows.
- **English and Persian** — interface language can be changed from the tray menu.

## Get the app

1. Open **[the latest Windows build](https://github.com/namep0001/ChronoDeskWidget/actions/workflows/build-windows.yml)**.
2. Select the latest successful run on the main branch.
3. Under **Artifacts**, download `ChronoDeskWidget-Windows`.
4. Extract the artifact and run `ChronoDeskWidget.exe`.

The artifact is built by GitHub Actions on a Windows runner. Artifacts are temporary downloads managed by GitHub and may expire.

## Run from source

**Requirements**
- Windows 10 or Windows 11
- .NET Framework 4.8
- Windows .NET Framework C# compiler, or Visual Studio Build Tools with the .NET desktop build tools

**Steps**
1. Download or clone this repository.
2. Run `Start-Widget.bat`.
3. The launcher builds the application if the executable is missing or source files are newer, then starts it.

To build without launching, run `build.bat`. The executable is written to `bin\\Release\\ChronoDeskWidget.exe`.

You can also open `ChronoDeskWidget.csproj` in Visual Studio and build the Release configuration.

## Repository layout

```text
ChronoDeskWidget/
├── .github/workflows/
│   └── build-windows.yml       # Windows build and downloadable artifact
├── index.html                  # Project landing page
├── Source/
│   ├── Program.cs              # Application entry point
│   ├── MainForm.cs             # Main window and widget UI
│   ├── LocalStore.cs            # Local settings and data persistence
│   ├── TgjuClient.cs            # Market-rate retrieval and parsing
│   └── Generate-AppIcon.ps1    # Creates the executable icon during builds
├── ChronoDeskWidget.csproj
├── Start-Widget.bat
├── build.bat
└── README.md
```

## Data and privacy

Tasks, notes, settings, window position, and the startup error log are stored in:

```text
%LOCALAPPDATA%\\ChronoDeskWidget\\
```

Task and note data stay on the computer. Market-rate requests use the following public TGJU pages:

- [Dollar](https://www.tgju.org/profile/price_dollar_rl)
- [Euro](https://www.tgju.org/profile/price_eur)
- [18-karat gold](https://www.tgju.org/profile/geram18)

TGJU quotes are sourced in Rial. Toman display divides the source value by ten. Dollar display uses the live USD quote as its conversion reference. Internet access is required for market rates; unavailable source data is shown as unavailable rather than fabricated.

## Build automation

Every push to `main` and every pull request targeting `main` triggers the Windows build workflow. A successful run publishes the executable as the `ChronoDeskWidget-Windows` artifact.

The project targets .NET Framework 4.8 and does not require third-party runtime packages.
