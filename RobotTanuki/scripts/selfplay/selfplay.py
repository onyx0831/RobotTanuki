"""2つのUSIエンジンを対局させて勝率を測る。通常はDockerコンテナ内でrun.shから呼ばれる。

usage: selfplay.py <engineA> <engineB> [--games N] [--byoyomi MS] [--workers W] [--seed S]
"""
import argparse
import math
import os
import queue
import random
import subprocess
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor, as_completed

import shogi

MAX_PLIES = 256

# 止まったエンジンを検出するための待ち時間。深さ1の探索が長引く局面で誤検出しないよう長めにとる。
GO_TIMEOUT_SEC = 120

# Ctrl+Cで中断したときにまとめて止めるため、起動したエンジンを覚えておく。
running_procs = []
procs_lock = threading.Lock()


class Engine:
    def __init__(self, path, label):
        self.label = label
        self.proc = subprocess.Popen([path], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                     stderr=subprocess.DEVNULL, text=True, bufsize=1)
        with procs_lock:
            running_procs.append(self.proc)
        # readlineはタイムアウトを指定できないため、別スレッドで読んでキュー経由で受け取る。
        self.lines = queue.Queue()
        threading.Thread(target=self._read_lines, daemon=True).start()
        self.last_info_string = None
        self.send("usi")
        self.wait_for("usiok")
        self.send("isready")
        self.wait_for("readyok")

    def _read_lines(self):
        for line in self.proc.stdout:
            self.lines.put(line)
        self.lines.put(None)

    def send(self, line):
        self.proc.stdin.write(line + "\n")
        self.proc.stdin.flush()

    def wait_for(self, prefix):
        deadline = time.monotonic() + GO_TIMEOUT_SEC
        while True:
            try:
                line = self.lines.get(timeout=max(0, deadline - time.monotonic()))
            except queue.Empty:
                raise TimeoutError(f"{GO_TIMEOUT_SEC}秒以内に{prefix}が返ってきませんでした")
            if line is None:
                raise RuntimeError("エンジンが終了しました")
            # エンジンは探索中の例外をinfo stringで知らせてから投了するので、普通の投了と見分けられるよう残す。
            if line.startswith("info string "):
                self.last_info_string = line[len("info string "):].strip()
            if line.startswith(prefix):
                return line.strip()

    def go(self, moves, byoyomi):
        """指し手と、goを送ってからbestmoveが返るまでのミリ秒を返す。"""
        self.last_info_string = None
        self.send("position startpos" + (" moves " + " ".join(moves) if moves else ""))
        start = time.monotonic()
        self.send(f"go btime 0 wtime 0 byoyomi {byoyomi}")
        best = self.wait_for("bestmove").split()[1]
        return best, (time.monotonic() - start) * 1000

    def close(self):
        self.proc.kill()
        self.proc.wait()


class TimeStats:
    """エンジンごとの思考時間。深さ1の探索はキャンセルできず秒読みを超えることがあり、
    超えた側はその分多く考えられて公平でなくなるため、超過を集計して確かめられるようにする。"""

    def __init__(self, byoyomi):
        self.byoyomi = byoyomi
        self.lock = threading.Lock()
        self.stats = {}

    def record(self, label, elapsed_ms):
        with self.lock:
            s = self.stats.setdefault(label, {"moves": 0, "over": 0, "max_ms": 0.0})
            s["moves"] += 1
            s["over"] += elapsed_ms > self.byoyomi
            s["max_ms"] = max(s["max_ms"], elapsed_ms)

    def lines(self):
        return [f"{label}: {s['moves']}手、秒読み（{self.byoyomi}ms）超過 {s['over']}回、最長 {s['max_ms'] / 1000:.2f}秒"
                for label, s in sorted(self.stats.items())]


def random_opening(rng, plies):
    board = shogi.Board()
    moves = []
    for _ in range(plies):
        move = rng.choice(list(board.legal_moves))
        board.push(move)
        moves.append(move.usi())
    return moves


def play_game(engine_black, engine_white, opening, byoyomi, time_stats):
    """先手から見た結果（1=先手勝ち, 0=後手勝ち, 0.5=引き分け）、終局理由、棋譜を返す。"""
    board = shogi.Board()
    moves = list(opening)
    for move in moves:
        board.push_usi(move)
    engine_black.send("usinewgame")
    engine_white.send("usinewgame")
    while True:
        side_to_move_loses = 0 if board.turn == shogi.BLACK else 1
        if not any(True for _ in board.legal_moves):
            return side_to_move_loses, "詰み", moves
        # 連続王手の千日手はエンジン側もまだ扱っていないため、区別せず引き分けにする。
        if board.is_fourfold_repetition():
            return 0.5, "千日手", moves
        if len(moves) >= MAX_PLIES:
            return 0.5, f"{MAX_PLIES}手到達", moves
        engine = engine_black if board.turn == shogi.BLACK else engine_white
        # エンジンが落ちた・止まった・読めない手を返した場合も、それまでの結果を失わないよう手番側の負けとして続ける。
        try:
            best, elapsed_ms = engine.go(moves, byoyomi)
            time_stats.record(engine.label, elapsed_ms)
            if best == "resign":
                detail = f"（info string: {engine.last_info_string}）" if engine.last_info_string else ""
                return side_to_move_loses, "投了" + detail, moves
            move = shogi.Move.from_usi(best)
        except Exception as e:
            return side_to_move_loses, f"エラー（{type(e).__name__}: {e}）", moves
        if move not in board.legal_moves:
            return side_to_move_loses, f"反則（{best}）", moves
        board.push(move)
        moves.append(best)


def elo(score):
    score = min(max(score, 1e-6), 1 - 1e-6)
    return 400 * math.log10(score / (1 - score))


def print_summary(results, time_stats):
    n = len(results)
    if n == 0:
        print("終局した対局はありません")
        return
    wins = results.count(1)
    losses = results.count(0)
    draws = n - wins - losses
    score = sum(results) / n
    summary = f"A vs B: {wins}勝 {losses}敗 {draws}分  得点率 {score:.3f}"
    # 結果が全て同じだとばらつきが0になり、区間の幅も0になって確実な差（または互角）に見えてしまう。
    if len(set(results)) > 1:
        se = math.sqrt(sum((s - score) ** 2 for s in results) / n / n)
        summary += f"  Elo {elo(score):+.0f}（95%区間 {elo(score - 1.96 * se):+.0f} 〜 {elo(score + 1.96 * se):+.0f}）"
    print(summary)
    for line in time_stats.lines():
        print(line)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("engine_a")
    parser.add_argument("engine_b")
    parser.add_argument("--games", type=int, default=2, help="奇数だと、最後の開始局面はAが先手の1局だけになる")
    parser.add_argument("--byoyomi", type=int, default=1000)
    # CPUのコア数を超えて同時に対局させると、思考中のエンジン同士でCPUを取り合い、読みが浅くなる。
    parser.add_argument("--workers", type=int, default=4)
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--opening-plies", type=int, default=6)
    args = parser.parse_args()

    # 同じ開始局面を先後入れ替えて2局ずつ指し、開始局面と先手番の有利不利を打ち消す。
    rng = random.Random(args.seed)
    openings = [random_opening(rng, args.opening_plies) for _ in range((args.games + 1) // 2)]
    jobs = [(i, opening, a_is_black) for i, opening in enumerate(openings) for a_is_black in (True, False)][:args.games]

    lock = threading.Lock()
    results = []
    time_stats = TimeStats(args.byoyomi)
    local = threading.local()
    stopping = threading.Event()

    def run(job):
        if stopping.is_set():
            return
        i, opening, a_is_black = job
        if not hasattr(local, "a"):
            local.a = Engine(args.engine_a, "A")
            local.b = Engine(args.engine_b, "B")
        black, white = (local.a, local.b) if a_is_black else (local.b, local.a)
        result, reason, moves = play_game(black, white, opening, args.byoyomi, time_stats)
        if reason.startswith("エラー"):
            # 落ちた・止まったエンジンを次の対局に持ち越さないよう、作り直す。
            local.a.close()
            local.b.close()
            del local.a, local.b
        a_score = result if a_is_black else 1 - result
        with lock:
            results.append(a_score)
            print(f"[{len(results)}/{len(jobs)}] 開始局面{i} Aが{'先手' if a_is_black else '後手'}: "
                  f"A={a_score}（{reason}、{len(moves)}手）", flush=True)
            print(f"  position startpos moves {' '.join(moves)}", flush=True)

    executor = ThreadPoolExecutor(max_workers=min(args.workers, len(jobs)))
    futures = [executor.submit(run, job) for job in jobs]
    try:
        for future in as_completed(futures):
            future.result()
    except KeyboardInterrupt:
        # 対局中のスレッドは外から止められないので、エンジンを落として途中までの結果を出し、そのまま終了する。
        stopping.set()
        with procs_lock:
            for proc in running_procs:
                proc.kill()
        with lock:
            print("中断しました。終局した対局までの結果:")
            print_summary(results, time_stats)
        # os._exitは標準出力のバッファを書き出さずに終了するため、先に書き出す。
        sys.stdout.flush()
        os._exit(130)
    executor.shutdown()
    print_summary(results, time_stats)


if __name__ == "__main__":
    main()
