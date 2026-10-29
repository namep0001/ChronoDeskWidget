# ChronoDesk Widget — validation report

## Changes covered in this revision

- Added tray-menu visibility switches for Clock and dates, Calendar, Tasks, Pomodoro, Quick note, and Live market rates.
- Saved each visibility choice in `%LOCALAPPDATA%\ChronoDeskWidget\settings.ini` with all six widgets enabled by default for existing installations.
- Removed the visible header/title strip and its language, always-on-top, and hide buttons. Those controls are available from the tray menu; tray options include Show, Hide, Language, Keep on top, Start with Windows, and Exit.
- Added Windows theme detection using the Windows system light/dark preference, the Start/taskbar accent toggle under Personalize, and Windows accent color registry values. The palette refreshes on relevant Windows preference notifications and is checked every ten seconds as a fallback.
- Kept the widget draggable from the clock/date labels, section headings, and blank card surfaces.
- Converted source IRR integer values to toman by dividing by 10 before display. English uses `T`; Persian uses `تومان (T)`.
- Cleared control references when rebuilding the sections, avoiding stale references when a widget is hidden while market-rate work is completing.
- Avoided market-rate auto-refresh when the rate widget is disabled; manually enabling the rate widget refreshes it.

## Checks completed in the authoring environment

- ZIP archive integrity check.
- Project XML parses successfully.
- Static source checks confirm all six tray visibility entries map to the six section keys and each section is conditionally constructed.
- Static source check confirms market values are divided by ten and the visible labels/status no longer refer to IRR/ریال.
- Static source check confirms the old title-bar and header-button fields/methods are absent.
- C# delimiter/brace scan completed.

## Not verified here

The authoring environment is Linux and has no Windows C# compiler, MSBuild, or .NET Framework toolchain installed. The modified Windows executable could not be compiled or GUI-tested in this environment. Build the source on Windows using `build.bat`, then run the smoke tests below.

## Windows smoke-test checklist

1. Run `build.bat`, then launch `bin\Release\ChronoDeskWidget.exe`.
2. Confirm there is no visible header, title, icon, or language/topmost/close button on the widget.
3. Right-click the tray icon and toggle each of the six widget sections; confirm each choice persists after restarting.
4. Turn all sections off, then turn one back on through the tray. Confirm the widget hides when no sections are enabled and reappears when a widget is enabled.
5. Drag the widget using the clock/date labels or blank card surfaces.
6. Change Windows between light and dark taskbar styles, and enable/disable the taskbar accent color. Confirm the widget palette updates without restarting.
7. Check that the dollar, euro, and 18K gold values are divided by ten and labeled as T / تومان (T).
8. Change language, toggle always-on-top, hide/show, exit, and toggle Windows startup from the tray.
9. With Tasks, Notes, and Pomodoro enabled, verify their existing add/complete/remove, persistence, and timer functions still work.
10. Measure working set and idle CPU in Task Manager after several minutes idle.