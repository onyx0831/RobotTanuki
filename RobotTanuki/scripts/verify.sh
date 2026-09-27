#!/usr/bin/env bash
# RobotTanukiの手動検証をまとめたスクリプト。
# 探索・指し手生成まわりを変更した際、既知の局面での挙動が壊れていないことを確認するために使う。
# 想定する「正解」の値は、これまでの開発の過程で実際に確認した結果を書き写したもの。
set -euo pipefail
cd "$(dirname "$0")/.."

fail() {
  echo "NG: $1" >&2
  exit 1
}

run_usi() {
  printf "%s" "$1" | dotnet run --no-build 2>/dev/null
}

count_moves() {
  echo "$1" | tr ' ' '\n' | grep -c '▲\|△' || true
}

echo "== build =="
dotnet build

echo "== dotnet format チェック =="
dotnet format --verify-no-changes

echo "== 初期局面: usi/isready/go =="
out=$(run_usi $'usi\nisready\nposition startpos\ngo\nquit\n')
echo "$out" | grep -q "^usiok" || fail "usiokが返ってこない"
echo "$out" | grep -q "^readyok" || fail "readyokが返ってこない"
echo "$out" | grep -q "^bestmove 7g7f" || fail "初期局面のbestmoveが7g7fではない: $out"

echo "== 初期局面の合法手数（30通り） =="
out=$(run_usi $'position startpos\ngeneratemove\nquit\n')
count=$(count_moves "$out")
[ "$count" = "30" ] || fail "初期局面の合法手数が30ではない: $count"

echo "== 王手回避（自玉が取られる手は除外、4通り） =="
out=$(run_usi $'position sfen 4r4/9/9/9/9/9/9/9/4K3P b - 1\ngeneratemove\nquit\n')
count=$(count_moves "$out")
[ "$count" = "4" ] || fail "王手回避局面の合法手数が4ではない: $count"

echo "== 最多合法手局面（593通り） =="
out=$(run_usi $'position sfen 8R/kSS1S1K2/4B4/9/9/9/9/9/3L1L1L1 b RBGSNLP3g3n17p 1\ngeneratemove\nquit\n')
count=$(count_moves "$out")
[ "$count" = "593" ] || fail "最多合法手局面の合法手数が593ではない: $count"

echo "== 千日手（飛車損の後手は4回目の同一局面に戻して引き分けにする） =="
out=$(run_usi $'position sfen 8k/9/9/9/9/9/9/9/K6R1 b - 1 moves 2i3i 1a1b 3i2i 1b1a 2i3i 1a1b 3i2i 1b1a 2i3i 1a1b 3i2i\ngo\nquit\n')
echo "$out" | grep -q "^bestmove 1b1a" || fail "千日手になる1b1aを選ばない: $out"

echo "== 連続王手の千日手（王手をかけ続けた相手の負けになる手を選ぶ） =="
out=$(run_usi $'position sfen 1r6k/9/9/9/9/9/9/9/K8 w - 1 moves 8a9a 9i8i 9a8a 8i9i 8a9a 9i8i 9a8a 8i9i 8a9a 9i8i 9a8a 8i9i 8a9a\ngo\nquit\n')
echo "$out" | grep -q "^bestmove 9i8i" || fail "相手の連続王手の千日手になる9i8iを選ばない: $out"

echo "== 連続王手の千日手（自分が王手をかけ続けて負けになる手は選ばない） =="
out=$(run_usi $'position sfen 8k/9/9/9/9/9/9/9/K7R w 2g2s 1 moves 1a2a 1i2i 2a1a 2i1i 1a2a 1i2i 2a1a 2i1i 1a2a 1i2i 2a1a\ngo\nquit\n')
echo "$out" | grep -q "^bestmove" || fail "bestmoveが返ってこない: $out"
if echo "$out" | grep -q "^bestmove 2i1i"; then fail "自分の連続王手の千日手になる2i1iを選んだ: $out"; fi

echo "全項目OK"
