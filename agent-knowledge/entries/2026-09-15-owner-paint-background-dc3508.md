---
id: rm-20260915-owner-paint-background
topic: settings-ui
type: failure
status: active
maturity: candidate
created: 2026-09-15
last_verified: 2026-09-15
source_commit: "ce606ee"
related_files:
  - src/CopilotKeyFighter/Visuals.cs
  - tests/CopilotKeyFighter.Tests/PaintTests.cs
  - docs/verification.md
tags:
  - winforms
  - painting
  - black-background
---
# 独自描画の背景を DrawToBitmap の事前消去に依存させない

## Conclusion

UiButton・UiCheck・ModeSelector.Choice は自分の描画領域を全面的に塗る必要がある。図形や文字だけを描くと、通常の再描画でチェック欄や丸い角の外側に黒いバッファが残る。PaintCanvas で親の不透明な背景色を先に塗り、Opaque と ResizeRedraw を指定する。

## Scope

Applicable: このアプリの平坦な単色パネルに配置する独自描画の操作部品。

Do not apply: グラデーション・背景画像・透過合成が必要な親では、単色塗りだけで背景を再現できるとは限らない。標準の文字編集部品を不用意に独自描画へ置換しない。

## Evidence

- 2026-09-15 のユーザー提供画像では、すべての動作モードでチェック欄に黒帯、ボタンや選択肢の角に黒い塗り残りが見えた。
- 通常の DrawToBitmap プレビューでは先に背景が描画され、不具合が見えていなかった。
- PaintTests.OwnerDrawControlsCoverTheirBufferWithoutBackgroundErase は黒い描画先に OnPaint だけを呼ぶ。ce606ee のコードでは背景色を期待した箇所が黒のままで失敗し、全面塗りを追加したコードでは成功した。
- テストは親背景色2種、チェック状態、有効／無効、横幅変更を組み合わせて確認する。

## Verification

1. 独自 OnPaint の先頭で PaintCanvas が呼ばれ、全面が塗られることを読む。
2. PaintTests を実行する。完成済みの DrawToBitmap 画像だけを根拠にしない。
3. 実画面で選択切り替え・サイズ変更時の背景と丸い角を確認する。
