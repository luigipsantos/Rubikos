using System;
using System.Linq;
using NUnit.Framework;
using Rubikos.Core;

namespace Rubikos.Tests
{
    public class CubeStateTests
    {
        private const string SolvedString =
            "UUUUUUUUU" + "RRRRRRRRR" + "FFFFFFFFF" + "DDDDDDDDD" + "LLLLLLLLL" + "BBBBBBBBB";

        private static CubeState Run(string moves)
        {
            var cube = new CubeState();
            cube.Apply(MoveNotation.Parse(moves));
            return cube;
        }

        [Test]
        public void NewCube_IsSolved()
        {
            var cube = new CubeState();
            Assert.IsTrue(cube.IsSolved);
            Assert.AreEqual(SolvedString, cube.ToFaceletString());
        }

        [TestCase("U")] [TestCase("D")] [TestCase("R")] [TestCase("L")] [TestCase("F")]
        [TestCase("B")] [TestCase("M")] [TestCase("E")] [TestCase("S")]
        public void FourQuarterTurns_ReturnToSolved(string move)
        {
            var cube = Run($"{move} {move} {move} {move}");
            Assert.AreEqual(SolvedString, cube.ToFaceletString());
        }

        [TestCase("R")] [TestCase("R'")] [TestCase("U2")] [TestCase("F")] [TestCase("L'")]
        [TestCase("D")] [TestCase("B2")] [TestCase("M")] [TestCase("E'")] [TestCase("S2")]
        public void MoveThenInverse_ReturnsToSolved(string notation)
        {
            Move move = MoveNotation.Parse(notation)[0];
            var cube = new CubeState();

            cube.Apply(move);
            Assert.IsFalse(cube.IsSolved, "the move should scramble the cube");

            cube.Apply(move.Inverse);
            Assert.IsTrue(cube.IsSolved);
        }

        [Test]
        public void SexyMove_RepeatedSixTimes_ReturnsToSolved()
        {
            var cube = Run(string.Concat(Enumerable.Repeat("R U R' U' ", 6)));
            Assert.IsTrue(cube.IsSolved);
        }

        [Test]
        public void SingleR_MatchesKnownReferenceString()
        {
            string expected = "UUFUUFUUF" + "RRRRRRRRR" + "FFDFFDFFD" + "DDBDDBDDB" + "LLLLLLLLL" + "UBBUBBUBB";
            Assert.AreEqual(expected, Run("R").ToFaceletString());
        }

        [Test]
        public void SingleU_MovesTopRowsLeftward()
        {
            // Clockwise U: the front's top row goes to the left, left's to back, back's to right, right's to front.
            string expected = "UUUUUUUUU" + "BBBRRRRRR" + "RRRFFFFFF" + "DDDDDDDDD" + "FFFLLLLLL" + "LLLBBBBBB";
            Assert.AreEqual(expected, Run("U").ToFaceletString());
        }

        [Test]
        public void WholeCubeRotation_StillCountsAsSolved()
        {
            // R M' L' turns the entire cube like the "x" rotation. Colors change place, but each face stays uniform.
            var cube = Run("R M' L'");
            Assert.IsTrue(cube.IsSolved);
            Assert.AreEqual(SolvedString, cube.ToFaceletString());
        }

        [Test]
        public void Notation_RoundTrip()
        {
            const string text = "R U2 F' M E' S2 B' L D2";
            Assert.AreEqual(text, MoveNotation.ToNotation(MoveNotation.Parse(text)));
        }

        [Test]
        public void Parse_UnknownLetter_Throws()
        {
            Assert.Throws<FormatException>(() => MoveNotation.Parse("R X"));
        }
    }
}