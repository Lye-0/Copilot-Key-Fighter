# Current design — Copilot Key Fighter 0.1

## Application boundaries

The application is a .NET 10 Windows Forms resident for Windows x64. Program owns the per-session named mutex and forwards subsequent launches to a message-only window. Resident owns the hook, tray, settings, cancellation lifetime, and action routing. First run and invalid settings are disabled. No installed application or key is selected implicitly.

SettingsStore uses versioned JSON, validates before replacing the settings file, and preserves unreadable input for recovery. Startup modifies only the current user's CopilotKeyFighter Run entry. The executable must remain at a stable location. A new launch refreshes an enabled registration. Legacy ChatGPTLauncher is detected through its named mutex; enabling both is refused. The app does not edit PowerToys configuration.

## Keyboard contract

KeyboardHook owns a dedicated idle message loop. It recognizes physical Left Win + Left Shift + F23 only. Injected events are ignored. CopilotGesture tracks F23 down/repeat/up and keeps a captured up suppressed even when settings change midway. Modifier events and unrelated keys always pass through. No modifier synthesis occurs in capture or foreground activation.

All enabled actions use the existing internal registered VK_NONAME down/up pair. It supplies a real WM_HOTKEY path for foreground activation and consumes the chord before a bare Win release can open Start. Registration is transactional, failures leave capture disabled, and an outstanding notification expires after 1.5 seconds. This technique needs physical-key validation on each supported environment; it is not a universal foreground-permission bypass.

Application actions run immediately from accepted WM_HOTKEY. Key actions wait for observed physical keys to be up (ReleaseGate), with a maximum 2-second deadline and 10-ms checks only during a pending gesture. A generation change, other physical key down, changed foreground window, or disable cancels pending input. No queue is accumulated. PendingGesture contains the pure decision policy. ReleaseGate additionally checks Windows state for modifiers, mouse buttons and the target key, but does not treat reserved/IME virtual-key state or the suppressed F23 async state as physical key evidence.

KeySequence emits modifiers down, primary key down/up, then modifiers up in reverse order. One SendInput batch is submitted with an identifying tag. Extended keys carry the extended flag. Accepted-prefix recovery releases only injected keys and avoids keys now physically down. If recovery itself is rejected, further key sending is locked until restart and an error is shown. Windows input restrictions still apply; this does not promise successful injection or recovery in protected desktops.

## Application identity and activation

AppCatalog enumerates AppsFolder on a separate STA thread, releases COM objects, and returns immutable targets. The picker displays names and lazily resolved shell icons. TargetKind distinguishes packaged AUMIDs, executable paths, and Shell registrations.

AppLauncher uses IApplicationActivationManager for packaged targets, direct Process.Start with UseShellExecute=false for exe targets, and a Shell item default verb for other registrations. Shell registrations do not accept arbitrary launch arguments in the UI. Runtime does not invoke PowerShell, cmd.exe, explorer.exe, or a generated shortcut as a launcher.

Existing windows are matched by exact process AUMID or executable path, not title or package family alone. Hosted packaged windows inspect descendants. The topmost matching unowned, visible, uncloaked window is preferred. This does not resolve every launcher/child-process architecture. Without a matching window an action launches once; while opening, repeated requests are coalesced. An existing-window activation failure is reported without another launch. New windows are searched for up to five seconds; no window is not necessarily an error for command tools.

## Visual direction

The adopted cyan key is the principal brand asset. A navy identity panel carries the expressive copy, while the light task area contains native controls. The three action choices use an accessible radio group; selecting, keyboard navigation, focus and checked state remain native semantics. Save remains in a fixed footer. Advanced arguments and working directory are disclosed on demand. At smaller widths the identity panel yields space to the task.

Color, button, selection and dropdown styling share Visuals tokens. The picker uses the actual installed-app icons. Native scrollbars and file dialogs deliberately follow Windows settings; high-contrast mode bypasses custom control colors. No continuous animation or rendering loop runs in the tray resident. Popup interaction, high-contrast changes at runtime and additional DPI settings require manual verification beyond rendered previews.

## Distribution

scripts/package.ps1 builds the Windows x64 self-contained single-file executable, includes GPL-3.0, notices, runtime licenses, and creates a ZIP plus SHA256. GitHub Actions tests/packages on branch/PR and creates a draft prerelease on a version tag. The same commit's source archive is attached. Publishing a draft is a separate release operation. No updater, service, signing, or installer is implemented.

The historical migration plan remains under docs/plans; this document describes current code. See docs/verification.md for what was actually exercised and what remains to validate.

