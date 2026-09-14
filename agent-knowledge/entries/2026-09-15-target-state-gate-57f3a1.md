---
id: rm-20260915-target-state-gate
topic: keyboard-safety
type: failure
status: active
maturity: candidate
created: 2026-09-15
last_verified: 2026-09-15
source_commit: "34511f7"
related_files:
  - src/CopilotKeyFighter/ReleaseGate.cs
  - src/CopilotKeyFighter/Resident.cs
  - tests/CopilotKeyFighter.Tests/DeletePipelineTests.cs
  - docs/verification.md
tags:
  - delete
  - asynchronous-state
  - native-test
---
# 対象キーの非同期状態だけで送信を待つと Delete が送れなくなる

## Conclusion

非修飾キーの送信先を GetAsyncKeyState(target) だけで「ユーザーが保持している」と判断しない。このプロジェクトの実送信テストでは、観測した物理キーは解放済みでも Delete の非同期状態が送信前から true で、解放ゲートが期限切れまで閉じ続けた。現在はフックが観測した物理入力で非修飾キーを判断し、Windows の非同期状態による追加ガードは修飾キーとマウスボタンに限定する。

## Scope

Applicable: Copilot 操作から単発の非修飾キー／ショートカットを送信する解放ゲート。

Do not apply: 修飾キーの安全確認を削除する理由にはならない。実際に観測した物理キーの押下、別の入力、前面変更のガードを無効化しない。非同期状態が残った原因を特定の常駐ツールだと断定しない。

## Evidence

- docs/verification.md の Native input reproduction and correction: GetAsyncKeyState(Delete) は内部通知を送る前から true。物理入力保持と修飾キーの診断は空だった。
- 修正前の DeletePipelineTests は、同一 foreground / input sequence のまま TargetHeld=true で時間切れとなり abc が変化しなかった。
- ReleaseGate の対象キー非同期判定を外した後、同じネイティブフック・Resident・SendInput 経路で abc → bc が3回連続成功。各回の modifier 解放も確認した。
- 合成された Copilot 入力の受け入れは隔離テストインスタンスだけの明示的なオプション。本番インスタンスは引き続き注入された Copilot 入力を無視する。

## Verification

1. ReleaseGate が実入力と修飾キーの保護を維持し、非修飾 target の async 状態だけで待たないことを読む。
2. 常駐を停止して DeletePipelineTests と SafetyTests を実行する。
3. 実機の入力結果は独立して確認する。純粋な状態機械テストだけで実送信成功を主張しない。

## Reuse Evidence

2026-09-15: The user confirmed operation verification was complete after the corrected native-input build. This is evidence for the reported Delete scenario on that setup, not universal hardware compatibility.

