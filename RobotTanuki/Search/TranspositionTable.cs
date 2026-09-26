namespace RobotTanuki
{
    /// <summary>
    /// 置換表に格納する評価値が、真の値に対してどういう関係にあるか。
    /// アルファベータ法では、打ち切りが起きたノードの評価値は真の値そのものではなく、
    /// 上限または下限としてしか分からないため、これを区別して記録する必要がある。
    /// </summary>
    public enum TranspositionTableBound
    {
        Exact,
        LowerBound,
        UpperBound,
    }

    public struct TranspositionTableEntry
    {
        public ulong Hash;
        public int Depth;
        public int Value;
        public Move BestMove;
        public TranspositionTableBound Bound;
    }

    public class TranspositionTable
    {
        private readonly TranspositionTableEntry[] entries;

        /// <param name="entryCount">2のべき乗であること（ビットマスクでインデックスを求めるため）</param>
        public TranspositionTable(int entryCount)
        {
            entries = new TranspositionTableEntry[entryCount];
        }

        public bool TryGet(ulong hash, out TranspositionTableEntry entry)
        {
            entry = entries[Index(hash)];
            return entry.Hash == hash;
        }

        public void Store(ulong hash, int depth, int value, Move bestMove, TranspositionTableBound bound)
        {
            entries[Index(hash)] = new TranspositionTableEntry
            {
                Hash = hash,
                Depth = depth,
                Value = value,
                BestMove = bestMove,
                Bound = bound,
            };
        }

        private int Index(ulong hash)
        {
            return (int)(hash & (ulong)(entries.Length - 1));
        }
    }
}
