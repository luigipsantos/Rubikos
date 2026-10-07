using System;
using System.Linq;
using NUnit.Framework;
using Rubikos.Core;

namespace Rubikos.Tests
{
    public class ScramblerTests
    {
        [Test]
        public void Generate_ReturnsRequestedLength()
        {
            Assert.AreEqual(20, Scrambler.Generate(20, new Random(1)).Count);
        }

        [Test]
        public void Generate_ZeroLength_ReturnsEmpty()
        {
            Assert.AreEqual(0, Scrambler.Generate(0, new Random(1)).Count);
        }

        [Test]
        public void Generate_UsesOnlyOuterFaces()
        {
            var moves = Scrambler.Generate(200, new Random(7));
            Assert.IsTrue(moves.All(m => m.Layer != 0), "middle slices must not appear in a scramble");
        }

        [Test]
        public void Generate_NeverTurnsTheSameAxisTwiceInARow()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var moves = Scrambler.Generate(30, new Random(seed));
                for (int i = 1; i < moves.Count; i++)
                    Assert.AreNotEqual(moves[i - 1].Axis, moves[i].Axis, $"seed {seed}, move {i}");
            }
        }

        [Test]
        public void Generate_SameSeed_GivesSameScramble()
        {
            string a = MoveNotation.ToNotation(Scrambler.Generate(20, new Random(42)));
            string b = MoveNotation.ToNotation(Scrambler.Generate(20, new Random(42)));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Generate_DifferentSeeds_GiveDifferentScrambles()
        {
            string a = MoveNotation.ToNotation(Scrambler.Generate(20, new Random(1)));
            string b = MoveNotation.ToNotation(Scrambler.Generate(20, new Random(2)));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void Scramble_LeavesCubeUnsolved()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var cube = new CubeState();
                cube.Apply(Scrambler.Generate(20, new Random(seed)));
                Assert.IsFalse(cube.IsSolved, $"seed {seed}");
            }
        }

        [Test]
        public void Scramble_ThenReversedInverses_ReturnsToSolved()
        {
            // This is exactly what Undo will do, one move at a time.
            for (int seed = 0; seed < 50; seed++)
            {
                var moves = Scrambler.Generate(25, new Random(seed));
                var cube = new CubeState();
                cube.Apply(moves);

                cube.Apply(Enumerable.Reverse(moves).Select(m => m.Inverse));

                Assert.IsTrue(cube.IsSolved, $"seed {seed}");
            }
        }
    }
}