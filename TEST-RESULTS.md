# ChronoDesk Widget — validation report

## Project files

- WinForms project targeting .NET Framework 4.8.
- Main source: Source\MainForm.cs.
- A compressed copy of the main source is kept at Source\MainForm.cs.gz.b64 for clean-download recovery. The packaging workflow synchronizes the compressed backup and the SHA-256 expected by restore-mainform.ps1.
- Build and launch scripts restore the readable source if it is missing.

## Current redesign

- Replaced the vertical stack with a compact two-column layout. Clock and the single calendar span the widget width; Tasks/Pomodoro and Quick Note/Market Rates use paired columns.
- Removed the small local-time hint under the two date lines. Gregorian and Solar Hijri dates remain visible; the calendar itself switches between the two systems in one panel.
- Added light and dark palettes driven by Windows app-theme preference and Windows accent color.
- Added persisted window coordinates so the widget reopens where it was last moved.
- Added tray submenus for visible widgets, opacity and blur, market rates, language, and font. The individual widget visibility toggles are nested under Visible widgets; no layout mode is offered.
- Added widget opacity choices (50–100%) and an Acrylic/blur-behind toggle.
- Added Toman (T) / Rial (R) price-unit selection independent of English/Persian UI language.
- Added font choices: Segoe UI, Arial, Calibri, Tahoma, Verdana, and Microsoft YaHei UI.
- Pomodoro controls now provide Start/Pause, Reset, and a rest/cup switch between 25-minute focus and 5-minute rest. No option/ellipsis menu is present.
- Task count remains completed/total. Tasks and notes use internal scroll areas with Windows light/dark scrollbar theme hints.
- Quick Note saves locally automatically and has a Save As button for exporting a UTF-8 .txt file to a user-selected location.
- Retained TGJU market-data parser fix: correct Persian/English current-price parsing, first formatted price only, and normalized Persian/Arabic numerals.

## Automated Windows build

- Commit 1e6465d46960648e7d52962ce66b96ac874abacd compiled successfully on GitHub Actions Windows runner.
- The first attempt at the same redesign caught a WinForms API issue (Timer.IsDisposed); this was corrected before the successful build.
- Successful Windows build workflow: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41031981582
- The workflow creates the ChronoDeskWidget-Windows artifact containing bin\Release\ChronoDeskWidget.exe.

## Not yet verified interactively

The automated build confirms compilation, not all Windows visual behaviors. The following still require manual GUI testing on Windows 10/11:
- Compare the widget in both Windows light and dark modes and confirm accent colors match the current Windows accent.
- Try opacity and blur on/off. Acrylic blur depends on Windows composition/version; check appearance at the opacity levels.
- Drag the window to another location, close the app with tray > Exit, and relaunch; confirm it restores the previous location.
- Change the font and restart; confirm the selected font persists and Persian glyph fallback is acceptable.
- Toggle every item in Visible widgets and confirm visibility persists.
- Verify that the single calendar switches between Solar Hijri and Gregorian and that month navigation/today selection work.
- Add/complete/remove multiple tasks; confirm the completed/total counter and internal scrollbar.
- Enter a long quick note; confirm autosave after typing and verify Save As creates a readable UTF-8 .txt file at the chosen location.
- Switch the market-rate unit between Toman and Rial while testing both UI languages; refresh quotes and verify the values and suffixes.
- Test Pomodoro Start/Pause, Reset, the focus/rest switch, and automatic transition at the end of each interval.
- Measure working set and idle CPU in Task Manager after several minutes idle.

## Known platform considerations

- The app is a frameless WinForms desktop window. Backdrop blur uses the Windows composition accent policy; availability and rendering vary by Windows build and desktop composition state.
- Scrollbar appearance uses the native Windows Explorer/DarkMode_Explorer theme hints. Exact scrollbar coloring is controlled by Windows and may vary by system build.
- No Windows GUI session was available in this authoring environment, so visual effects and interactive UX have not been claimed as manually verified.
