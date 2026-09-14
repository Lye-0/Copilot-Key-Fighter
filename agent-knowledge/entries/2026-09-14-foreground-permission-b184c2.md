---
id: rm-20260914-foreground-permission
topic: foreground-activation
type: constraint
status: active
maturity: candidate
created: 2026-09-14
last_verified: 2026-09-14
source_commit: "88aa9b3"
related_files:
  - src/CopilotKeyFighter/ForegroundHotkey.cs
  - src/CopilotKeyFighter/Resident.cs
  - src/CopilotKeyFighter/AppLauncher.cs
  - tests/CopilotKeyFighter.Tests/WindowTests.cs
tags:
  - win32
  - foreground
  - testing
---
# テストホストからの前面化と実ホットキー経路を区別する

## Conclusion

SetForegroundWindow をテストホストから呼べることと、OS から前面化を許可されることは別。ウィンドウの識別・復元が成功しても、直接の前面化は拒否され得る。実キー経路の ForegroundHotkey を消して単なる PostMessage に統合しない。前面化拒否を理由にアプリを再起動してはならない。

## Scope

Applicable: 常駐プロセスからのアプリ前面化、テストの設計、既存ウィンドウ優先処理。

Do not apply: この内部 VK_NONAME 通知で全 Windows 環境の前面化が保証されるとは扱わない。管理者・保護画面の制約を回避する根拠にしない。

## Evidence

- 現行コードは物理フックから私用メッセージを送り、登録済み内部キーによる WM_HOTKEY からアプリを開く。
- 2026-09-14 の WindowTests 実行で、テスト用ウィンドウの検出は成功し、最小化からの復元後に前面化拒否が発生した。
- WindowTests はこの拒否を許容する一方、復元と同一ウィンドウの維持を検証する。実ホットキー成功の試験とは明確に分ける。

## Verification

1. AppLauncher の既存ウィンドウ失敗経路が新規起動に落ちないことを確認する。
2. ForegroundHotkey の登録失敗時解放・未到着時の期限、Resident の通知経路を確認する。
3. 自動テストに加えて、実 Copilot キーで背面・最小化からの復帰を確認する。
