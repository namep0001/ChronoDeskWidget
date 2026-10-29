# ChronoDesk Widget

A lightweight Windows 10/11 desktop widget inspired by Windows 7 gadgets. It uses WinForms and .NET Framework 4.8, with no third-party runtime dependencies.

## Features

- Live clock, Gregorian date, and Solar Hijri date.
- Navigable Gregorian and Solar Hijri calendars.
- Persistent local tasks (add, complete/reopen, remove).
- Pomodoro focus timer with pause, reset, and break switching.
- Persistent quick notes.
- TGJU free-market USD and EUR and 18-karat gold-per-gram quotes, displayed in toman (IRR values divided by 10).
- English and Persian interface.
- System-tray controls for showing/hiding sections, language, always-on-top, startup, refresh, and exit.
- Frameless widget with a Windows system theme/accent-aware palette.
- Low-overhead WinForms implementation with cached fonts and disposed rebuilt controls.

## Download

Open this repository and select Code → Download ZIP. This downloads the full source project.

## Build and run

1. Extract the repository ZIP to a normal folder.
2. Double-click Start-Widget.bat. It restores the main source file if necessary, builds the app, and launches it.
3. The compiled program is generated at bin\Release\ChronoDeskWidget.exe.

To compile manually, run build.bat. Alternatively, in Windows PowerShell 5.1 run Set-ExecutionPolicy -Scope Process Bypass, then .\build.ps1.

The project targets .NET Framework 4.8. If no C# compiler is available, install Visual Studio Build Tools with the .NET desktop build tools.

### Main source restoration

Source\MainForm.cs.gz.b64 contains the compressed main form source. restore-mainform.ps1 reconstructs Source\MainForm.cs and verifies the restored file against its SHA-256 checksum before the build continues. The resulting C# source is an ordinary editable MainForm.cs file.

## Market data

Rates require internet access to TGJU. The app reads:
- https://www.tgju.org/profile/price_dollar_rl
- https://www.tgju.org/profile/price_eur
- https://www.tgju.org/profile/geram18

If TGJU is unreachable or changes its page structure, the app displays an unavailable/partial status rather than inventing a current quote.

## Local data and privacy

Tasks, notes, settings, and the startup error log are stored under %LOCALAPPDATA%\ChronoDeskWidget\.

Tasks and notes remain local; the app only makes network requests for market prices.

## Testing

The source was checked for project structure and build-script consistency. The authoring environment does not include Windows .NET Framework tooling, so compile and GUI testing must be performed on Windows 10/11. See TEST-RESULTS.md for the Windows smoke-test checklist.
