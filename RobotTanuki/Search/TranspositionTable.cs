using System.Runtime.CompilerServices;

namespace RobotTanuki
{
    /// <summary>
    /// 置換表に格納する評価値が、真の値に対してどういう関係にあるか。
    /// アルファベータ法では、打ち切りが起きたノードの評価値は真の値そのものではなく、
    /// 上限または下限としてしか分からないため、これを区別して記録する必要がある。
    /// </summary>
    public enum TranspositionTableBound : byte
    {
        Exact,
        LowerBound,
        UpperBound,
    }

    /// <summary>
    /// 参照を含む大きな配列はGCのたびに走査され、置換表を大きくするほど遅くなるため、
    /// 指し手も整数に詰めて、参照を含まない16バイトの構造体にしている。
    /// </summary>
    public struct TranspositionTableEntry
    {
        public ulong Hash;
        public int Value;
        public ushort BestMove16;
        public sbyte Depth;
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

        /// <summary>
        /// 指定したメガバイト数に収まる、最も大きい2のべき乗の要素数でテーブルを作る。
        /// </summary>
        public static TranspositionTable FromMegabytes(int megabytes)
        {
            long maxEntryCount = (long)megabytes * 1024 * 1024 / Unsafe.SizeOf<TranspositionTableEntry>();
            long entryCount = 1;
            while (entryCount * 2 <= maxEntryCount)
            {
                entryCount *= 2;
            }
            return new TranspositionTable((int)entryCount);
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
                Value = value,
                BestMove16 = bestMove.ToUshort(),
                Depth = (sbyte)depth,
                Bound = bound,
            };
        }

        private int Index(ulong hash)
        {
            return (int)(hash & (ulong)(entries.Length - 1));
        }
    }
}
