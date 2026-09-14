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

## 2026-09-15 release-gate correction

A physical Copilot attempt reached the action handler, kept the same input sequence and foreground window, but timed out waiting for key release. The old gate scanned the entire asynchronous virtual-key range. Which virtual key blocked that attempt was not recorded, so the precise hardware cause is not established.

ReleaseGate now uses physical down/up observed by the hook, including the suppressed F23 pair, plus explicit Windows-state checks for modifiers, mouse buttons and the target key. A regression test checks that reserved/IME/F23 async states alone do not block sending, while real held keys and modifiers do. The internal notification pair release test also passed. All 23 tests passed. Physical Delete output on this updated build is awaiting user confirmation.

The runtime preview is separate from bin/Debug. Before debugging an actual resident in Visual Studio, exit any running instance from the tray. A running Debug executable is locked by Windows and must exit before it can be rebuilt. Diagnostics overwrite only last-gesture.json with the last action result; they do not record text or a keyboard history.

### Follow-up: actual cancellation source

User testing of the release-gate change still produced no Delete. The subsequent record showed SameInput=false, SameWindow=true, TimedOut=false, PhysicalF23=true and LastInterference=LShiftKey. This establishes that Left Shift arriving within a captured Copilot chord was treated as an unrelated action. Ignoring repeats alone was insufficient because the hook could first observe that modifier down after F23 was captured using Windows modifier state.

The cancellation policy now excludes Left Win/Left Shift belonging to the captured F23 chord, while preserving cancellation by unrelated keys and new modifier actions after the chord ends. All 25 tests passed, including the delayed-modifier sequence. Debug build passed with zero warnings/errors. Physical Delete confirmation is still pending for this follow-up build.

### Native input reproduction and correction

The isolated native pipeline test reproduced the failure with no observed physical keys held and no modifiers down. Its diagnostic showed TargetHeld=true; GetAsyncKeyState(Delete) was already true before the test's first internal notification. The old gate therefore waited for the requested Delete itself and never sent it. The origin of that pre-existing asynchronous state is unknown; no claim is made that a specific external tool caused it.

The gate now protects observed physical presses plus asynchronous modifier/mouse state, without blocking on the non-modifier target's asynchronous state alone. The internal mask pair is also emitted directly on capture, before UI scheduling, to avoid a late mask after physical Win release. The foreground-change cancellation remains enabled.

DeletePipelineTests passed through the native hook, resident dispatch and SendInput and changed its owned TextBox from abc to bc three consecutive times. Modifier-release checks passed after each attempt. Production still ignores injected Copilot chords; tagged injection is enabled only on the isolated test instance. All 26 tests passed, and Debug built with zero warnings/errors. This materially strengthens the previous pure-state-machine checks; physical hardware confirmation is still separate.

### User confirmation

On 2026-09-15, the user confirmed that operation verification was complete after the native-input correction. This closes the reported physical Delete failure for the user's current setup; broader release gates remain separate.

