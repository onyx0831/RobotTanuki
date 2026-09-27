"""2つのUSIエンジンを対局させて勝率を測る。通常はDockerコンテナ内でrun.shから呼ばれる。

usage: selfplay.py <engineA> <engineB> [--games N] [--byoyomi MS] [--workers W] [--seed S]
"""
import argparse
import math
import random
import subprocess
import threading
from concurrent.futures import ThreadPoolExecutor

import shogi

MAX_PLIES = 256


class Engine:
    def __init__(self, path):
        self.proc = subprocess.Popen([path], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                     stderr=subprocess.DEVNULL, text=True, bufsize=1)
        self.send("usi")
        self.wait_for("usiok")
        self.send("isready")
        self.wait_for("readyok")

    def send(self, line):
        self.proc.stdin.write(line + "\n")
        self.proc.stdin.flush()

    def wait_for(self, prefix):
        while True:
            line = self.proc.stdout.readline()
            if not line:
                raise RuntimeError("エンジンが終了しました")
            if line.startswith(prefix):
                return line.strip()

    def go(self, moves, byoyomi):
        self.send("position startpos" + (" moves " + " ".join(moves) if moves else ""))
        self.send(f"go btime 0 wtime 0 byoyomi {byoyomi}")
        return self.wait_for("bestmove").split()[1]


def random_opening(rng, plies):
    board = shogi.Board()
    moves = []
    for _ in range(plies):
        move = rng.choice(list(board.legal_moves))
        board.push(move)
        moves.append(move.usi())
    return moves


def play_game(engine_black, engine_white, opening, byoyomi):
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
        best = engine.go(moves, byoyomi)
        if best == "resign":
            return side_to_move_loses, "投了", moves
        move = shogi.Move.from_usi(best)
        if move not in board.legal_moves:
            return side_to_move_loses, f"反則（{best}）", moves
        board.push(move)
        moves.append(best)


def elo(score):
    score = min(max(score, 1e-6), 1 - 1e-6)
    return -400 * math.log10(1 / score - 1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("engine_a")
    parser.add_argument("engine_b")
    parser.add_argument("--games", type=int, default=2)
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
    local = threading.local()

    def run(job):
        i, opening, a_is_black = job
        if not hasattr(local, "a"):
            local.a = Engine(args.engine_a)
            local.b = Engine(args.engine_b)
        black, white = (local.a, local.b) if a_is_black else (local.b, local.a)
        result, reason, moves = play_game(black, white, opening, args.byoyomi)
        a_score = result if a_is_black else 1 - result
        with lock:
            results.append(a_score)
            print(f"[{len(results)}/{len(jobs)}] 開始局面{i} Aが{'先手' if a_is_black else '後手'}: "
                  f"A={a_score}（{reason}、{len(moves)}手）", flush=True)
            print(f"  position startpos moves {' '.join(moves)}", flush=True)

    with ThreadPoolExecutor(max_workers=min(args.workers, len(jobs))) as executor:
        list(executor.map(run, jobs))

    n = len(results)
    wins = results.count(1)
    losses = results.count(0)
    draws = n - wins - losses
    score = sum(results) / n
    summary = f"A vs B: {wins}勝 {losses}敗 {draws}分  得点率 {score:.3f}"
    # 全勝・全敗だとEloは無限大に発散し、ばらつきも0になって区間が意味をなさない。
    if 0 < score < 1:
        se = math.sqrt(sum((s - score) ** 2 for s in results) / n / n)
        summary += f"  Elo {elo(score):+.0f}（95%区間 {elo(score - 1.96 * se):+.0f} 〜 {elo(score + 1.96 * se):+.0f}）"
    print(summary)


if __name__ == "__main__":
    main()
