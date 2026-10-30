# ChronoDesk Widget — validation report

## Project files

- WinForms project targeting .NET Framework 4.8.
- Main source: Source\MainForm.cs.
- A compressed copy of the main source is kept at Source\MainForm.cs.gz.b64 for clean-download recovery. The packaging workflow synchronizes the compressed backup and the SHA-256 expected by restore-mainform.ps1.
- Build and launch scripts restore the readable source if it is missing.

## Current UI redesign

- Fixed three paired rows: Clock / Calendar, Tasks / Pomodoro, and Quick Notes / Market Rates.
- The window computes its column count from visible widgets and contracts to a single tile when no visible pair requires two columns. It expands to two columns when a pair is enabled.
- Each card uses a clipped rounded window region and a smooth rounded outline. This replaces the earlier square/painted-corner treatment.
- Added monochrome GDI+ vector icons to the six widget headings and rate rows.
- Section headings now use title case.
- Clock shows both Gregorian and Solar Hijri dates. Calendar is a smaller single panel with Solar Hijri / Gregorian switching, compact month navigation, and circular selected/today date markers.
- Added a circular Pomodoro progress ring; it fills with elapsed focus/rest time. The timer switches between 25-minute focus and 5-minute rest.
- Market Rates title does not display the data-source brand. Currency display choices are Dollar ($) and Toman (T). Dollar prices are calculated against the live USD quote; Toman prices divide the source Rial quote by ten. Rial is not available as a display option.
- Removed all blur controls and DWM backdrop blur code.
- Windows light/dark theme and accent-color support remain.
- Window position, opacity, widget visibility, selected currency, and font remain persisted locally.
- Task count, task scrolling, note autosave, and .txt Save As remain available.
- Tray menus retain widget visibility, opacity, market rate refresh/currency, language, font, always-on-top, Windows startup, and exit.

## Automated Windows build

- UI redesign commit: d7a7e772d3cdf4e42a42a6288a16610abdbf59e4.
- Windows build workflow: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41040036447
- Result: successful. The Windows runner completed the application build and uploaded the executable artifact.
- Source packaging workflow: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41040036540
- Result: successful. The compressed MainForm source and restore checksum were regenerated.
- Executable artifact: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41040036447/artifacts/12969637132

## Not yet verified interactively

The automated build confirms compilation, not all Windows visual behaviors. The following still require manual GUI testing on Windows 10/11:
- Compare rounded card rendering, icon alignment, calendar spacing, and the Pomodoro ring against the reference mockup.
- Confirm the layout when all widgets are visible, when one tile is visible, and when an individual widget pair is only partially enabled.
- Compare the widget in Windows light and dark modes and confirm accent colors match the current Windows accent.
- Change opacity, move the window, and restart; confirm persisted position and settings.
- Change the font and restart; confirm the selected font persists and Persian glyph fallback is acceptable.
- Toggle every item in Visible widgets and confirm visibility persists.
- Verify calendar switching, month navigation, today selection, and circular date highlighting.
- Add/complete/remove tasks; confirm the completed/total counter and scrolling.
- Enter a long quick note; confirm autosave and verify Save As creates a readable UTF-8 .txt file.
- Switch between Dollar ($) and Toman (T); refresh quotes and verify conversions.
- Test Pomodoro Start/Pause, Reset, focus/rest switch, circular progress, and automatic transitions.
- Measure working set and idle CPU in Task Manager after several minutes idle.

## Known platform considerations

- This is a frameless WinForms desktop window. Custom rounded regions and GDI+ vector icons are used for the card surfaces and headings.
- Scrollbar appearance uses native Windows Explorer/DarkMode_Explorer theme hints. Exact scrollbar coloring is controlled by Windows and may vary by system build.
- No Windows GUI session was available in this authoring environment, so visual effects and interactive UX have not been claimed as manually verified.
