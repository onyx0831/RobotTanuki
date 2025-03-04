# RobotTanuki
USI対応将棋思考エンジン

# 新しいプロジェクトを始める場合
- .NETをインストール
- .NETのプロジェクトを作る
  ```bash
  # new directory内で
  dotnet new console
  ```

# buildする
- プロジェクトファイル(.csproj)が出来たらビルドしてみる
  ```bash
  dotnet build
  ```
- dotnet publishで公開
  ```bash
  dotnet publish -c Release -r win-x64 --self-contained true -o build
  ```

# デバックする
- build/exeファイルを開く

初期局面のセット
- プロンプト上で「position startpos」または「position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1」を入力
- 「d」を入力
- 「generatemove」で有効な指し手を示す
  （最多合法手局面：「position sfen 8R/kSS1S1K2/4B4/9/9/9/9/9/3L1L1L1 b RBGSNLP3g3n17p 1」）