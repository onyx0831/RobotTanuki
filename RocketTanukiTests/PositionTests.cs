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
                foreach (var move in MoveGenerator.Generate(position, null))
                {
                    using (var mover = new Mover(position, move)) { }
                    Assert.AreEqual(hash, position.Hash);
                }
            }
        }
    }
}
