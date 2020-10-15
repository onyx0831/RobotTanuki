﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RocketTanuki;

namespace RocketTanukiTests
{
    [TestClass]
    public class PositionTests
    {
        [TestMethod]
        public void Position_Hash()
        {
            foreach (var sfen in new[] { Position.StartposSfen, Position.MatsuriSfen, Position.MaxSfen })
            {
                var position = new Position();
                position.Set(sfen);

                long hash = position.Hash;
                foreach (var move in MoveGenerator.Generate(position, null, false))
                {
                    using (var mover = new Mover(position, move)) { }
                    Assert.AreEqual(hash, position.Hash);
                }
            }
        }

        [TestMethod]
        public void Position_King()
        {
            var position = new Position();
            position.Set(Position.StartposSfen);

            Assert.AreEqual(4, position.BlackKingFile);
            Assert.AreEqual(8, position.BlackKingRank);
            Assert.AreEqual(4, position.WhiteKingFile);
            Assert.AreEqual(0, position.WhiteKingRank);
        }

        [TestMethod]
        public void Position_ToSfenString()
        {
            foreach (var expected in new[] { Position.StartposSfen, Position.MatsuriSfen, Position.MaxSfen })
            {
                var position = new Position();
                position.Set(expected);
                var actual = position.ToSfenString();
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
