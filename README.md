# ChronoDesk Widget

A lightweight Windows 10/11 desktop widget inspired by Windows 7 gadgets. It uses WinForms and .NET Framework 4.8, with no third-party runtime dependencies.

## Features

- Frameless, draggable desktop widget with a compact two-column layout: full-width clock and switchable calendar, followed by paired Tasks/Pomodoro and Notes/Market Rates panels.
- Clock displays Gregorian and Solar Hijri dates together. The single navigable calendar switches between Solar Hijri and Gregorian views.
- Windows light/dark theme adaptation with the current Windows accent color; compact card styling and rounded section panels.
- Persisted window position, opacity (50–100%), acrylic/blur-behind toggle, and font selection.
- Persistent local tasks with completion counts, add/complete/remove, and themed scrollable list.
- Pomodoro focus and rest timer (25-minute focus / 5-minute rest), with start/pause, reset, and a dedicated focus/rest switch.
- Persistent quick note with automatic local saving and Save As export to a UTF-8 .txt file anywhere.
- TGJU free-market USD and EUR and 18-karat gold-per-gram quotes. Choose Rial (R) or Toman (T) independently of the interface language.
- English and Persian interface.
- Tray submenus for visible widgets, widget opacity/blur, market-rate refresh and units, language, and font; also includes always-on-top, Windows startup, and exit.
- No app header, top toolbar, separate layout mode, or outer scrolling area.

## Appearance controls

The tray menu provides:
- **Visible widgets** — independently show or hide Clock, Calendar, Tasks, Pomodoro, Quick Note, and Market Rates.
- **Widget opacity** — 100%, 90%, 80%, 70%, 60%, or 50%, plus **Blur background**.
- **Market rates** — refresh TGJU quotes and switch between Toman (T) and Rial (R).
- **Language** — English or Persian.
- **Font** — Segoe UI, Arial, Calibri, Tahoma, Verdana, or Microsoft YaHei UI.

The application follows the Windows app light/dark preference and Windows accent color. Acrylic blur availability and rendering depend on the Windows version and desktop composition settings. Opacity and position settings are stored locally and restored on the next launch.

## Download

Open this repository and select Code → Download ZIP. This downloads the full source project.

## Build and run

1. Extract the repository ZIP to a normal folder.
2. Double-click Start-Widget.bat. It restores the main source file if necessary, builds the app, and launches it.
3. The compiled program is generated at bin\Release\ChronoDeskWidget.exe.

To compile manually, run build.bat. Alternatively, in Windows PowerShell 5.1 run Set-ExecutionPolicy -Scope Process Bypass, then .\build.ps1.

The project targets .NET Framework 4.8. If no C# compiler is available, install Visual Studio Build Tools with the .NET desktop build tools.

## Main source restoration

Source\MainForm.cs.gz.b64 is a compressed backup of the current main form source. restore-mainform.ps1 reconstructs Source\MainForm.cs and verifies the restored file against its SHA-256 checksum before the build continues. A GitHub Actions workflow keeps the compressed backup and checksum synchronized with the readable source.

## Market data

Rates require internet access to TGJU. The app reads:
- https://www.tgju.org/profile/price_dollar_rl
- https://www.tgju.org/profile/price_eur
- https://www.tgju.org/profile/geram18

TGJU values are available as Rial (R) or Toman (T); Toman values are divided by ten. If TGJU is unreachable or changes its page structure, the app displays an unavailable/partial status rather than inventing a current quote.

## Local data and privacy

Tasks, notes, settings, the saved widget position, and the startup error log are stored under %LOCALAPPDATA%\ChronoDeskWidget\.

Tasks and notes remain local; the app only makes network requests for market prices.

## Validation

The Windows GitHub Actions workflow compiles the project on a Windows runner and attaches the executable as a downloadable workflow artifact. Compilation does not replace interactive GUI checks on Windows 10/11; see TEST-RESULTS.md for the current build status and manual smoke-test checklist.
