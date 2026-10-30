# ChronoDesk Widget — validation report

## Project files

- WinForms project targeting .NET Framework 4.8.
- Main source: Source\MainForm.cs.
- A compressed copy of the main source is kept at Source\MainForm.cs.gz.b64 for clean-download recovery. The packaging workflow synchronizes the compressed backup and the SHA-256 expected by restore-mainform.ps1.
- Build and launch scripts restore the readable source if it is missing.

## Current UI design

- Widgets are laid out in visible order in a compact two-column flow. Hiding any widget pulls subsequent widgets up rather than leaving an empty cell. An odd final tile spans both columns.
- The window contracts to one tile width when only one widget is visible.
- Cards no longer use a native rounded window Region. Their outside corner pixels are painted using the actual parent background, then the rounded card surface and border are painted over it to avoid dark corner artifacts in the light theme.
- Monochrome vector icons are used for section headings and Pomodoro controls. Market rate icons use separate colored circular USD, EUR, and gold badges.
- Added a tray Theme submenu with System, Light, and Dark. System follows the Windows app-theme preference. Light/Dark override the surface palette while the Windows accent color remains automatically read from the system.
- Clock shows both Gregorian and Solar Hijri dates. Calendar switches between Solar Hijri and Gregorian with compact month navigation and circular date markers.
- Pomodoro's circular progress control draws the mode and timer text inside the ring. Remaining-time progress starts at 100% and shrinks to 0% as the countdown proceeds. Play/Pause, Reset, and Rest actions use custom monochrome vector controls.
- Market Rates title does not display the data-source brand. Currency display choices are Dollar ($) and Toman (T). Dollar prices are calculated against the live USD quote; Toman prices divide source Rial quotes by ten. Rial is not available as a display option.
- Opacity, theme selection, widget visibility, currency, font, and window position are saved locally.
- Task count, task scrolling, note autosave, and .txt Save As remain available.

## Automated Windows build

- UI change commit: e6bd7a3e495f3df858394952fd45ef8843e057fa.
- Vector button field-type correction: b1c16dc2572bd782a92caf12583afd4eeb2f0353.
- Windows build workflow: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41042821815
- Result: successful. The Windows runner completed the application build and uploaded the executable artifact.
- Source packaging workflow: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41042821726
- Result: successful. The compressed MainForm source and restore checksum were regenerated.
- Executable artifact: https://github.com/namep0001/ChronoDeskWidget/actions/runs/41042821815/artifacts/12971456069

## Not yet verified interactively

The automated build confirms compilation, not all Windows visual behaviors. The following still require manual GUI testing on Windows 10/11:
- Switch between System, Light, and Dark from the tray; confirm the accent color still follows Windows while the surfaces use the selected theme.
- Inspect card corners against both light and dark backgrounds, especially at the saved opacity setting.
- Disable a tile in each position and confirm later widgets move up without gaps. Test one visible widget and odd widget counts.
- Compare the compact calendar, selected/today circles, and date alignment with the reference mockup.
- Confirm the Pomodoro timer is centered inside the ring, that the ring starts full and shrinks while counting down, and that Play/Pause, Reset, and Rest icons work.
- Change opacity, move the window, and restart; confirm position and settings persist.
- Change the font and restart; confirm Persian glyph fallback is acceptable.
- Add/complete/remove tasks; confirm the completed/total counter and scrolling.
- Enter a long quick note; confirm autosave and verify Save As creates a readable UTF-8 .txt file.
- Switch between Dollar ($) and Toman (T); refresh quotes and verify conversions and colored badges.
- Test Pomodoro automatic transition from focus to rest and back.
- Measure working set and idle CPU in Task Manager after several minutes idle.

## Known platform considerations

- The custom rounded card treatment paints the outside corners with the parent background instead of relying on a native rounded window region.
- GDI+ draws the monochrome vector icons and the circular Pomodoro indicator.
- Scrollbar appearance uses native Windows Explorer/DarkMode_Explorer theme hints. Exact scrollbar coloring is controlled by Windows and may vary by system build.
- No Windows GUI session was available in this authoring environment, so visual effects and interactive UX have not been claimed as manually verified.
