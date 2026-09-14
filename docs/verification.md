# Verification — 0.1.0 preview

Date: 2026-09-14. Windows x64 / .NET SDK 10.0.401, runtime packs 10.0.12. This is a preview build; manual release gates below are not yet complete.

## Passed

- Final repository restore, Release build and self-contained/single-file publish succeeded. The final restore completed without the earlier sandbox-only NuGet audit retrieval warning.
- **21 xUnit tests passed, zero failures, zero skips.** This includes the original 2,576 assertions for F23 capture, unrelated-key pass-through, release orders and repeats.
- Key sequences for all 16 modifier combinations are balanced. Every accepted prefix has a matching recovery sequence. Recursive/reserved keys, modifier-only inputs and Ctrl+Alt+Delete are rejected.
- Pending input cancels on timeout, configuration generation change, disable, another key sequence or changed foreground window.
- Settings round-trip, atomic replacement, rejection without overwrite and corrupted-settings fail-open behavior passed.
- A real test window was matched by executable path, minimized and restored. The test host encountered foreground denial, which was reported without relaunch. This is not evidence that physical-key foreground activation passed.
- The settings editor preserved the selected Delete key and the radio selection semantics. Application and keyboard pages were rendered for inspection; sidebar text wrapping and fixed footer were corrected. Native dropdown printing does not fully represent the visible selected field, so popup appearance still needs live verification.
- App catalog enumeration returned **169 apps** on the development PC: 91 executable targets, 51 packaged targets, 27 Shell targets. Enumeration does not prove every entry can launch.
- The generated ZIP was extracted and its executable started. Starting again returned exit code 0 with **one resident process**. During a 3-second idle sample, measured CPU time increased by **0 ms**. The sample is short and does not establish long-term resource behavior.
- `--exit` ended the resident successfully. Tests did not alter the legacy launcher or PowerToys settings.
- ZIP generation includes one executable, GPL license, notices and the runtime package license files. SHA256 is emitted alongside the ZIP.

## Manual release gates — not yet run for this distribution build

- Physical Copilot key: application cold start, back-to-front, minimize/restore, already foreground, exit/relaunch, repeated pressing.
- Physical key output: ordinary key, Delete, F key, media key, Ctrl/Shift/Alt/Win combinations and JIS-specific keys. Check no stuck modifiers and no extra Start/Search/Copilot display.
- Full key-send partial-failure handling against real OS denial; pure sequence recovery is covered, but the OS may reject cleanup too. Such failure stops further sends and requires user recovery.
- PowerToys absent and coexisting with unrelated remaps; old launcher migration.
- Startup after actual logout/login, sleep/resume, and survival when the application that launched this resident closes.
- Packaged application, conventional Win32 app, launcher/child-process app, multiple-window app, deleted/moved target and elevated target.
- SDK-free clean Windows 11 installation, additional DPI settings, high contrast, keyboard navigation, open dropdown and picker scrolling.
- GitHub-hosted CI execution and draft release creation. No push, remote release or public publication was performed in this task.

## Reproduce

```powershell
dotnet test tests/CopilotKeyFighter.Tests/CopilotKeyFighter.Tests.csproj -c Release
./scripts/package.ps1 -Version 0.1.0
```

Physical testing should use a separate app if exiting the current conversation host would interrupt the session. Stop the legacy resident before enabling the distribution build; do not register both as active keyboard monitors.
