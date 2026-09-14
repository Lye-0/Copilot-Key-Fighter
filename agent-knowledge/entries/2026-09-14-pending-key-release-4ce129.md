---
id: rm-20260914-pending-key-release
topic: keyboard-safety
type: constraint
status: active
maturity: candidate
created: 2026-09-14
last_verified: 2026-09-14
source_commit: "88aa9b3"
related_files:
  - src/CopilotKeyFighter/KeyboardHook.cs
  - src/CopilotKeyFighter/Resident.cs
  - src/CopilotKeyFighter/PendingGesture.cs
  - tests/CopilotKeyFighter.Tests/SafetyTests.cs
tags:
  - modifiers
  - cancellation
  - copilot
---
# Copilot の物理修飾キーがキー割り当てへ混ざることを防ぐ

## Conclusion

このプロジェクトの入力元は Left Win + Left Shift + F23。キー割り当てを F23 down の瞬間に送ると、元の修飾キーが送信先のキーへ混ざる。キャプチャとキー送信を分け、解放後に送ること、待機中の別操作・設定変更・期限切れで取り消すことを一緒に維持する。物理修飾キーを人工的に解放する方式へ置き換えない。

## Scope

Applicable: 現行の Copilot 入力から単発キー／ショートカットを送信する経路。

Do not apply: アプリ起動を同じ理由で解放待ちへ遅らせる必要はない。別ハードウェアの入力方式に、そのまま一般化しない。

## Evidence

- 元のユーザー要件は Win/Shift key-up の保持を最優先とし、modifier stuck を避けることを指定している。
- KeyboardHook.OnKey は物理 modifier を通し、別の key down で Interference を増やす。
- Resident.Copilot は元の foreground と世代を保持し、PendingGesture.Evaluate で期限・別操作を判定する。
- SafetyTests.PendingGestureNeverSendsAfterInterferenceOrExpiry とシーケンスの全16修飾キー組合せ・部分送信回復が成功。実機注入の全成功を証明するものではない。

## Verification

1. 上記のフック・保留判定・送信が一緒に維持されていることを読む。
2. SafetyTests と LegacyTests を実行する。
3. 配布前に実キーの解放順、連打、同時入力、Win/Shift/Ctrl の通常動作を確認する。
