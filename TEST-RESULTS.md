# ChronoDesk Widget — validation report

## Project files published

- Added the WinForms project file, all supporting C# source files, build and launch scripts, README, and test checklist.
- The main form source is stored as Source/MainForm.cs.gz.b64 because the connected upload action does not accept a local file path or direct raw-file transfer. restore-mainform.ps1 reconstructs Source/MainForm.cs and verifies SHA-256 before any compile is attempted.
- Both build.bat and build.ps1 restore the main form automatically on a clean download.
- A GitHub Actions workflow is included to restore and commit the readable MainForm.cs source when the workflow can run with contents-write permissions.

## Feature changes in this revision

- Added tray-menu visibility switches for Clock and dates, Calendar, Tasks, Pomodoro, Quick note, and Live market rates.
- Saved each visibility choice in %LOCALAPPDATA%\ChronoDeskWidget\settings.ini with all six widgets enabled by default for existing installations.
- Removed the visible header/title strip and its language, always-on-top, and hide buttons. Those controls are in the tray menu.
- Added Windows theme detection using the Windows system light/dark preference, the Start/taskbar accent toggle, and Windows accent color registry values.
- Kept the widget draggable from clock/date labels, section headings, and blank card surfaces.
- Converted market rates from IRR to toman by dividing displayed values by ten.
- Avoided automatic market-rate refresh while the rates widget is hidden.
- Reduced redundant UI allocations using cached fonts, a shared tooltip, and disposal of dynamically recreated calendar/task controls.

## Checks performed in this authoring environment

- The downloaded ZIP project archive was listed and its files identified.
- Repository access, source file commits, project file commits, and README/script updates were confirmed through GitHub.
- The compressed MainForm source is guarded by a SHA-256 check against the original source hash: 18b1deade3907d820f20d4931c146a6299d081bd6f5e67384b801551cae981c0.
- The build scripts call the restoration script before compiling on a clean repository download.

## Not verified here

The authoring environment is Linux and has no Windows C# compiler, MSBuild, or .NET Framework toolchain installed. The modified Windows executable could not be compiled or GUI-tested in this environment. Build on Windows 10/11 and run the smoke tests below.

## Windows smoke-test checklist

1. From a clean GitHub Code → Download ZIP extraction, run Start-Widget.bat and confirm the source restoration checksum passes.
2. Run build.bat, then launch bin\Release\ChronoDeskWidget.exe.
3. Confirm there is no visible header, title, icon, or language/topmost/close button on the widget.
4. Right-click the tray icon and toggle each of the six widget sections; confirm each choice persists after restarting.
5. Turn all sections off, then turn one back on through the tray. Confirm the widget hides when no sections are enabled and reappears when a widget is enabled.
6. Drag the widget using clock/date labels or blank card surfaces.
7. Change Windows between light and dark taskbar styles, and enable/disable the taskbar accent color. Confirm the palette updates without restarting.
8. Check that dollar, euro, and 18K gold values are divided by ten and labeled as T / تومان (T).
9. Change language, toggle always-on-top, hide/show, exit, and toggle Windows startup from the tray.
10. With Tasks, Notes, and Pomodoro enabled, verify add/complete/remove, persistence, and timer functions still work.
11. Measure working set and idle CPU in Task Manager after several minutes idle.
