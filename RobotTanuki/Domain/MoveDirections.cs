using System.Collections.Generic;

namespace RobotTanuki
{
    public class Direction
    {
        public int DeltaFile { get; set; }
        public int DeltaRank { get; set; }
    }

    public class MoveDirection
    {
        public Direction Direction { get; set; }
        public bool Long { get; set; } = false;
    }

    /// <summary>
    /// 駒の種類ごとの移動方向のルール
    /// </summary>
    public static class MoveRules
    {
        private static Direction UpLeft = new Direction { DeltaFile = +1, DeltaRank = -1 };
        private static Direction Up = new Direction { DeltaFile = 0, DeltaRank = -1 };
        private static Direction UpRight = new Direction { DeltaFile = -1, DeltaRank = -1 };
        private static Direction Left = new Direction { DeltaFile = +1, DeltaRank = 0 };
        private static Direction Right = new Direction { DeltaFile = -1, DeltaRank = 0 };
        private static Direction DownLeft = new Direction { DeltaFile = +1, DeltaRank = +1 };
        private static Direction Down = new Direction { DeltaFile = 0, DeltaRank = +1 };
        private static Direction DownRight = new Direction { DeltaFile = -1, DeltaRank = +1 };

        public static List<MoveDirection>[] MoveDirections = {
            // NoPiece
            null,
            // BlackPawn
            new List<MoveDirection>{
                new MoveDirection{Direction=Up},
            },
            // BlackLance
            new List<MoveDirection>{
                new MoveDirection{Direction=Up,Long=true},
            },
            // BlackKnight
            new List<MoveDirection>{
                new MoveDirection{Direction=new Direction{DeltaRank=-2,DeltaFile=+1}},
                new MoveDirection{Direction=new Direction{DeltaRank=-2,DeltaFile=-1}},
            },
            // BlackSilver
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=DownRight},
            },
            // BlackGold
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=Down},
            },
            // BlackBishop
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft,Long=true},
                new MoveDirection{Direction=UpRight,Long=true},
                new MoveDirection{Direction=DownLeft,Long=true},
                new MoveDirection{Direction=DownRight,Long=true},
            },
            // BlackRook
            new List<MoveDirection>{
                new MoveDirection{Direction=Up,Long=true},
                new MoveDirection{Direction=Left,Long=true},
                new MoveDirection{Direction=Right,Long=true},
                new MoveDirection{Direction=Down,Long=true},
            },
            // BlackKing
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // BlackPromotedPawn
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=Down},
            },
            // BlackPromotedLance
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=Down},
            },
            // BlackPromotedKnight
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=Down},
            },
            // BlackPromotedSilver
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=Down},
            },
            // BlackHorse
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft,Long=true},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight,Long=true},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft,Long=true},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight,Long=true},
            },
            // BlackDragon
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up,Long=true},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left,Long=true},
                new MoveDirection{Direction=Right,Long=true},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down,Long=true},
                new MoveDirection{Direction=DownRight},
            },
            // WhitePawn
            new List<MoveDirection>{
                new MoveDirection{Direction=Down},
            },
            // WhiteLance
            new List<MoveDirection>{
                new MoveDirection{Direction=Down,Long=true},
            },
            // WhiteKnight
            new List<MoveDirection>{
                new MoveDirection{Direction=new Direction{DeltaRank=2,DeltaFile=+1}},
                new MoveDirection{Direction=new Direction{DeltaRank=2,DeltaFile=-1}},
            },
            // WhiteSilver
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhiteGold
            new List<MoveDirection>{
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhiteBishop
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft,Long=true},
                new MoveDirection{Direction=UpRight,Long=true},
                new MoveDirection{Direction=DownLeft,Long=true},
                new MoveDirection{Direction=DownRight,Long=true},
            },
            // WhiteRook
            new List<MoveDirection>{
                new MoveDirection{Direction=Up,Long=true},
                new MoveDirection{Direction=Left,Long=true},
                new MoveDirection{Direction=Right,Long=true},
                new MoveDirection{Direction=Down,Long=true},
            },
            // WhiteKing
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhitePromotedPawn
            new List<MoveDirection>{
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhitePromotedLance
            new List<MoveDirection>{
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhitePromotedKnight
            new List<MoveDirection>{
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhitePromotedSilver
            new List<MoveDirection>{
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight},
            },
            // WhiteHorse
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft,Long=true},
                new MoveDirection{Direction=Up},
                new MoveDirection{Direction=UpRight,Long=true},
                new MoveDirection{Direction=Left},
                new MoveDirection{Direction=Right},
                new MoveDirection{Direction=DownLeft,Long=true},
                new MoveDirection{Direction=Down},
                new MoveDirection{Direction=DownRight,Long=true},
            },
            // WhiteDragon
            new List<MoveDirection>{
                new MoveDirection{Direction=UpLeft},
                new MoveDirection{Direction=Up,Long=true},
                new MoveDirection{Direction=UpRight},
                new MoveDirection{Direction=Left,Long=true},
                new MoveDirection{Direction=Right,Long=true},
                new MoveDirection{Direction=DownLeft},
                new MoveDirection{Direction=Down,Long=true},
                new MoveDirection{Direction=DownRight},
            },
            // NumPieces
            null,
        };
    }
}
