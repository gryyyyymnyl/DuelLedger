# DuelLedger – Engineering Brief for Codex

## Goal
Shadowverse 戦績トラッカーを `DuelLedger.*` 命名で整理し、クロスプラットフォームでビルド可能にする。

## Tech baseline
- .NET 8 (net8.0)
- Avalonia UI (UI プロジェクト)
- OpenCvSharp4

## Non-goals
- 新機能追加
- 画像テンプレート資産の変更

## Constraints
- Windows 固有APIは使わない（将来は別プロジェクト化可）。
- `System.Drawing.*` 依存は削減対象。置換が難しい場合はインターフェイス分離。

## How to build
dotnet restore
dotnet build

## Structure (current)
Contracts/, Core/, Vision/, Detectors/Shadowverse/, Publishers/, UI/

## Acceptance style
- 受入条件は各Issueに記載。PRは「仕様照合→差分要約→テスト証跡→残課題」を記載。
