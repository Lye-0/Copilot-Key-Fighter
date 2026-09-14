# Current design — Copilot Key Fighter 0.1

## Application boundaries

The application is a .NET 10 Windows Forms resident for Windows x64. Program owns the per-session named mutex and forwards subsequent launches to a message-only window. Resident owns the hook, tray, settings, cancellation lifetime, and action routing. First run and invalid settings are disabled. No installed application or key is selected implicitly.

SettingsStore uses versioned JSON, validates before replacing the settings file, and preserves unreadable input for recovery. Startup modifies only the current user's CopilotKeyFighter Run entry. The executable must remain at a stable location. A new launch refreshes an enabled registration. Legacy ChatGPTLauncher is detected through its named mutex; enabling both is refused. The app does not edit PowerToys configuration.

## Keyboard contract

KeyboardHook owns a dedicated idle message loop. It recognizes physical Left Win + Left Shift + F23 only. Injected events are ignored. CopilotGesture tracks F23 down/repeat/up and keeps a captured up suppressed even when settings change midway. Modifier events and unrelated keys always pass through. No modifier synthesis occurs in capture or foreground activation.

All enabled actions use the existing internal registered VK_NONAME down/up pair. The hook now submits the preallocated pair before returning the captured F23 event; deferring this work to the UI message queue can be too late for the physical Win release. The hook still performs no file I/O, COM activation or explicit waits, and reports injection errors back to the UI. It supplies a real WM_HOTKEY path for foreground activation and consumes the chord before a bare Win release can open Start. Registration is transactional, failures leave capture disabled, and an outstanding notification expires after 1.5 seconds. This technique needs physical-key validation on each supported environment; it is not a universal foreground-permission bypass.

Application actions run immediately from accepted WM_HOTKEY. Key actions wait for observed physical keys to be up (ReleaseGate), with a maximum 2-second deadline and 10-ms checks only during a pending gesture. A generation change, a new unrelated physical key down, changed foreground window, or disable cancels pending input. Repeats of held keys do not count as new actions. While F23 is captured, Left Win and Left Shift belong to the Copilot chord and do not cancel it, even if their down events arrive after F23. Modifier up events continue to pass through unchanged. No queue is accumulated. PendingGesture contains the pure decision policy. ReleaseGate additionally checks Windows state for modifiers and mouse buttons. A non-modifier target may have a latched asynchronous state even without an observed physical press, so its asynchronous state alone does not block sending. Reserved/IME states and suppressed F23 async state are likewise not physical-key evidence.

KeySequence emits modifiers down, primary key down/up, then modifiers up in reverse order. One SendInput batch is submitted with an identifying tag. Extended keys carry the extended flag. Accepted-prefix recovery releases only injected keys and avoids keys now physically down. If recovery itself is rejected, further key sending is locked until restart and an error is shown. Windows input restrictions still apply; this does not promise successful injection or recovery in protected desktops.

## Application identity and activation

AppCatalog enumerates AppsFolder on a separate STA thread, releases COM objects, and returns immutable targets. The picker displays names and lazily resolved shell icons. TargetKind distinguishes packaged AUMIDs, executable paths, and Shell registrations.

AppLauncher uses IApplicationActivationManager for packaged targets, direct Process.Start with UseShellExecute=false for exe targets, and a Shell item default verb for other registrations. Shell registrations do not accept arbitrary launch arguments in the UI. Runtime does not invoke PowerShell, cmd.exe, explorer.exe, or a generated shortcut as a launcher.

Existing windows are matched by exact process AUMID or executable path, not title or package family alone. Hosted packaged windows inspect descendants. The topmost matching unowned, visible, uncloaked window is preferred. This does not resolve every launcher/child-process architecture. Without a matching window an action launches once; while opening, repeated requests are coalesced. An existing-window activation failure is reported without another launch. New windows are searched for up to five seconds; no window is not necessarily an error for command tools.

## Visual direction

Settings are organized in a single task-focused column: current applied status in the header, draft action and target in the body, and feedback/save controls in the bottom footer. The decorative sidebar is removed. Selected applications are displayed by name and icon; IDs, arguments and working directory are disclosed under advanced settings. Key selection uses a searchable dialog with readable labels instead of a raw Keys enumeration. Symbol-key labels use the current Windows keyboard layout.

SettingsForm keeps the applied status separate from its normalized draft baseline. Editing does not update the active-status label. Saving updates the baseline; reverting edits disables Save again. Resident forwards actual pause/configuration changes to the open form. A retained multiline test field allows testing the saved key action. Validation errors remain visible near Save; the footer can grow for wrapped feedback and remains anchored at small window sizes.

UiButton, UiCheck and ModeSelector provide consistent drawn selection, hover, disabled and focus states while retaining the native Button/CheckBox/RadioButton interaction and accessibility semantics. Search results, dialogs and the tray menu share the same palette. Windows text-editing internals, window chrome, scrollbars and file picking remain native intentionally for IME, accessibility and platform behavior. High-contrast colors are consulted during rendering. Actual screen-reader behavior and additional DPI configurations still need manual review.

Rendered application, keyboard and suppression pages and the key picker were reviewed. UI tests cover selected Delete preservation, draft/applied-state separation, revert/save behavior, pause status and Save visibility at a smaller size. The keyboard capture/sending pipeline is unchanged by this UI revision.
## Distribution

scripts/package.ps1 builds the Windows x64 self-contained single-file executable, includes GPL-3.0, notices, runtime licenses, and creates a ZIP plus SHA256. GitHub Actions tests/packages on branch/PR and creates a draft prerelease on a version tag. The same commit's source archive is attached. Publishing a draft is a separate release operation. No updater, service, signing, or installer is implemented.

The historical migration plan remains under docs/plans; this document describes current code. See docs/verification.md for what was actually exercised and what remains to validate.



The native DeletePipelineTests instantiate an isolated settings store and opt in to a tagged synthetic Copilot chord for that test hook only. Production leaves the tag at zero and rejects injected Copilot chords. The test focuses its own TextBox, verifies foreground ownership before injection, checks abc becomes bc three times, and checks modifier release. This test must run with the normal resident stopped because it exercises the actual global hook and internal hotkey registrations.



Owner-drawn controls explicitly fill their entire client area with the opaque parent surface at the start of OnPaint. They use Opaque and ResizeRedraw and do not rely on the background-erase pass of ButtonBase or DrawToBitmap. This covers checkbox labels and the area outside rounded button corners during partial repainting.

