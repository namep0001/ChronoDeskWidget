# ChronoDesk Widget

A lightweight Windows 10/11 desktop widget built with WinForms and .NET Framework 4.8. It has no third-party runtime dependencies.

## Features

- Frameless, draggable widget with three paired rows: Clock / Calendar, Tasks / Pomodoro, and Quick Notes / Market Rates.
- Responsive tile layout. The window contracts to one tile when only one widget is visible in a row and expands when its paired widget is enabled.
- Clock displays Gregorian and Solar Hijri dates together. The compact calendar switches between Solar Hijri and Gregorian views.
- Rounded, clipped card surfaces with monochrome vector icons, title-case headings, and Windows accent-color details.
- Windows light/dark theme adaptation and saved window position.
- Widget opacity choices from 50% to 100%; no backdrop blur option.
- Persistent local tasks with completion counts, add/complete/remove, and a scrollable list.
- Pomodoro focus/rest timer (25-minute focus / 5-minute rest), with circular progress, Start/Pause, Reset, and a focus/rest switch.
- Persistent quick notes with automatic local saving and Save As export to a UTF-8 .txt file.
- Live USD, EUR, and 18-karat gold-per-gram quotes. Choose display currency independently of interface language: Dollar ($) or Toman (T).
- English and Persian interface.
- Tray submenus for visible widgets, widget opacity, market-rate refresh and currency, language, and font; also includes always-on-top, Windows startup, and exit.

## Appearance controls

The tray menu provides:
- **Visible widgets** — independently show or hide Clock, Calendar, Tasks, Pomodoro, Quick Note, and Market Rates.
- **Widget opacity** — 100%, 90%, 80%, 70%, 60%, or 50%.
- **Market rates** — refresh quotes and choose Dollar ($) or Toman (T).
- **Language** — English or Persian.
- **Font** — Segoe UI, Arial, Calibri, Tahoma, Verdana, or Microsoft YaHei UI.

The application follows the Windows app light/dark preference and Windows accent color. Opacity, font, widget visibility, currency choice, and window position are stored locally.

## Download

Open this repository and select Code → Download ZIP to get the complete source project.

## Build and run

1. Extract the repository ZIP to a normal folder.
2. Double-click Start-Widget.bat. It restores the main source file if necessary, builds the app, and launches it.
3. The compiled program is generated at bin\Release\ChronoDeskWidget.exe.

To compile manually, run build.bat. Alternatively, in Windows PowerShell 5.1 run Set-ExecutionPolicy -Scope Process Bypass, then .\build.ps1.

The project targets .NET Framework 4.8. If no C# compiler is available, install Visual Studio Build Tools with the .NET desktop build tools.

## Main source restoration

Source\MainForm.cs.gz.b64 is a compressed backup of the current main form source. restore-mainform.ps1 reconstructs Source\MainForm.cs and verifies the restored file against its SHA-256 checksum before the build continues. A GitHub Actions workflow keeps the compressed backup and checksum synchronized with the readable source.

## Market data

Rates require internet access. The app reads current quotes from the following TGJU source pages:
- https://www.tgju.org/profile/price_dollar_rl
- https://www.tgju.org/profile/price_eur
- https://www.tgju.org/profile/geram18

The source quotes are provided in Rial. Toman display divides each quote by ten. Dollar display converts the EUR and gold quotes using the live USD quote as the reference; the USD quote displays as $1.00. Rial is not offered as a display currency. When the source is unreachable or its page structure changes, the app reports unavailable/partial data rather than inventing a quote.

## Local data and privacy

Tasks, notes, settings, the saved widget position, and the startup error log are stored under %LOCALAPPDATA%\ChronoDeskWidget\.

Tasks and notes remain local; the app only makes network requests for market prices.

## Validation

The Windows GitHub Actions workflow compiles the project on a Windows runner and attaches the executable as a downloadable workflow artifact. Compilation does not replace interactive GUI checks on Windows 10/11; see TEST-RESULTS.md for the current build status and manual smoke-test checklist.
