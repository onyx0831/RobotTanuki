#!/usr/bin/env bash
# 2つのブランチ（またはタグ）のエンジンをビルドし、selfplay.pyで対局させる。
# Dockerコンテナ内で、リポジトリが/repoに読み取り専用でマウントされている前提。
# usage: run.sh <新しいブランチ> <基準のブランチ> [selfplay.pyへの追加引数...]
set -euo pipefail

if [ $# -lt 2 ]; then
  echo "usage: <新しいブランチ> <基準のブランチ> [--games N] [--byoyomi MS] ..." >&2
  exit 1
fi
new_ref=$1
base_ref=$2
shift 2

# 2つのブランチを同時にビルドするため、マウントしたリポジトリをチェックアウトし直すのではなく、
# それぞれコンテナ内の別ディレクトリに取り出す。
build() {
  local ref=$1 dir=$2
  git init -q "$dir"
  git -C "$dir" fetch -q --depth 1 /repo "$ref"
  git -C "$dir" -c advice.detachedHead=false checkout -q FETCH_HEAD
  echo "== build $ref ($(git -C "$dir" rev-parse --short HEAD)) =="
  local log
  if ! log=$(dotnet build "$dir/RobotTanuki" -c Release -o "$dir/out" 2>&1); then
    echo "$log" >&2
    exit 1
  fi
}

build "$new_ref" /work/new
build "$base_ref" /work/base
python3 "$(dirname "$0")/selfplay.py" /work/new/out/RobotTanuki /work/base/out/RobotTanuki "$@"
