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
  scripts/      … 動作確認用のスクリプト（回帰確認・自己対局）
RobotTanuki.Tests/ … ユニットテスト（xUnit）。本体のプロジェクトに取り込まれないよう、RobotTanuki/の隣に置いている
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
- USIコマンドを直接打って確かめる場合は、ターミナルで`dotnet run`（またはpublishした`build/RobotTanuki`）を起動する

初期局面のセット
- プロンプト上で「position startpos」または「position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1」を入力
- 「d」を入力
- 「generatemove」で有効な指し手を示す
  （最多合法手局面：「position sfen 8R/kSS1S1K2/4B4/9/9/9/9/9/3L1L1L1 b RBGSNLP3g3n17p 1」）
- 「eval」で今の局面の評価値（手番側から見た値）を示す
- 「hash」で今の局面のZobristハッシュを示す

## 動作確認する
コマンドは`RobotTanuki/`ディレクトリで実行する。

- 探索・指し手生成まわりを変更したら、`./scripts/verify.sh` を実行して既知の局面での挙動が壊れていないことを確認する（ビルド・整形チェック・ユニットテスト・合法手数・bestmoveのチェックをまとめて行う）
- 指し手の変換など、USIから見えない内部の処理は、リポジトリ直下の`RobotTanuki.Tests`（xUnit）でテストする。単体で実行するときは`dotnet test ../RobotTanuki.sln`（リポジトリ直下の`RobotTanuki.sln`に本体とテストのプロジェクトをまとめている）
- 枝刈りなど探索結果が変わる変更では、`scripts/selfplay/`の自己対局で変更前後のエンジンを対局させて強さを比べる。Docker上で、指定した2つのブランチ（コミット済みの内容）をビルドして対局させる
  ```bash
  docker build -t robottanuki-selfplay scripts/selfplay
  docker run --rm -it -v "$(git rev-parse --show-toplevel)":/repo:ro robottanuki-selfplay <新しいブランチ> <基準のブランチ> --games 2 --byoyomi 1000
  ```
  ランダムな6手の開始局面から先後を入れ替えて指し、勝敗と棋譜（`position startpos moves ...`形式）、エンジンごとの秒読みの超過回数を出力する。ブランチ名はホストのローカルブランチを指すので、`main`を基準にするときは先に`git pull`しておく（または`origin/main`を指定する）。スクリプトを変更したら`docker build`し直す