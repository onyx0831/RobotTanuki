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
- exeファイルを開く
- プロンプト上で「position startpos」または「position sfen lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1」を入力
- 「d」を入力