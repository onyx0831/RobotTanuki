# RobotTanuki

USI対応将棋思考エンジン

## アーキテクチャ

責務ごとにディレクトリを分けている。

```
RobotTanuki/
  Program.cs    … エントリーポイント
  Usi/          … USIプロトコルの入出力（標準入出力のパース/整形のみ）
  Engine/       … 盤面・対局オプションの状態管理とオーケストレーション
  Domain/       … 将棋のルールそのもの（盤面・駒・指し手生成）
  Evaluation/   … 局面の評価
  Search/       … 探索
```

依存の向き（循環なし）:

```
Program → Usi → Engine → Search → Evaluation → Domain
```

- `Domain`は他レイヤーに依存しない
- `Usi`はUSIプロトコルの文字列（コマンドのパース/整形）を、`Engine`はUSIの文字列を一切知らないC#としてのAPIを、それぞれ担当する
- namespaceは本家RocketTanukiに合わせて`RobotTanuki`のまま。ディレクトリのみの整理

## 新しいプロジェクトを始める場合
- .NETをインストール
- .NETのプロジェクトを作る
  ```bash
  # new directory内で
  dotnet new console
  ```

## buildする
- プロジェクトファイル(.csproj)が出来たらビルドしてみる
  ```bash
  dotnet build
  ```
- dotnet publishで公開（Windows向け）
  ```bash
  dotnet publish -c Release -r win-x64 --self-contained true -o build
  ```
- dotnet publishで公開（Mac向け、Apple Siliconの場合）
  ```bash
  dotnet publish -c Release -r osx-arm64 --self-contained true -o build
  ```
  Intel Macの場合は`osx-arm64`の代わりに`osx-x64`を指定する。`build/`はgit管理外なので、Windows向け/Mac向けを同じフォルダに交互にpublishしても問題ない。

## デバッグする
- Windowsの場合はbuild/exeファイルを開く
- Macの場合はShogiHomeなどのGUIにbuild/RobotTanuki（拡張子なし）を思考エンジンとして登録する

初期局面のセット
- プロンプト上で「position startpos」または「position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1」を入力
- 「d」を入力
- 「generatemove」で有効な指し手を示す
  （最多合法手局面：「position sfen 8R/kSS1S1K2/4B4/9/9/9/9/9/3L1L1L1 b RBGSNLP3g3n17p 1」）

## 動作確認する
- 探索・指し手生成まわりを変更したら、`./scripts/verify.sh` を実行して既知の局面での挙動が壊れていないことを確認する（ビルド・整形チェック・合法手数・bestmoveのチェックをまとめて行う）