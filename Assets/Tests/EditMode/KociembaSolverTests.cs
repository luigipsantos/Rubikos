using System.IO;
using NUnit.Framework;
using Rubikos.Core;
using Rubikos.Solver;

namespace Rubikos.Tests
{
    public class KociembaSolverTests
    {
        private static string TableFolder => Path.Combine(UnityEngine.Application.persistentDataPath, "KociembaTables");
        private KociembaSolver solver;

        [OneTimeSetUp]
        public void PrepareSolver()
        {
            solver = new KociembaSolver(TableFolder, refineSeconds: 0f);
            solver.PrepareAsync().Wait(); // the first run ever builds the tables, which takes a while
            Assert.AreEqual(SolverStatus.Ready, solver.Status, solver.StatusMessage);
        }

        private static CubeState Scrambled(int seed, int length = 25)
        {
            var cube = new CubeState();
            cube.Apply(Scrambler.Generate(length, new System.Random(seed)));
            return cube;
        }

        private static void AssertSolves(CubeState cube, SolveResult result)
        {
            Assert.IsTrue(result.Success, result.Error);
            CubeState copy = cube.Clone();
            copy.Apply(result.Moves);
            Assert.IsTrue(copy.IsSolved, "following the solution must solve the cube");
        }

        [Test]
        public void SolvedCube_NeedsNoMoves()
        {
            SolveResult result = solver.Solve(new CubeState().ToFaceletString());

            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(0, result.Moves.Count);
        }

        [TestCase("R")] [TestCase("U'")] [TestCase("F2")] [TestCase("L")] [TestCase("D'")] [TestCase("B2")]
        public void SingleMove_IsSolvedInAFewMoves(string notation)
        {
            var cube = new CubeState();
            cube.Apply(MoveNotation.Parse(notation));

            SolveResult result = solver.Solve(cube.ToFaceletString());

            AssertSolves(cube, result);
            Assert.LessOrEqual(result.Moves.Count, 3);
        }

        [Test]
        public void RandomScrambles_AreSolved([Range(0, 9)] int seed)
        {
            CubeState cube = Scrambled(seed);

            SolveResult result = solver.Solve(cube.ToFaceletString());

            AssertSolves(cube, result);
            Assert.LessOrEqual(result.Moves.Count, 22);
        }

        [Test]
        public void MiddleSliceMoves_DoNotConfuseTheSolver()
        {
            // M, E and S move the centers. Our facelet string follows the centers, so this must still work.
            CubeState cube = Scrambled(123);
            cube.Apply(MoveNotation.Parse("M E' S2 M"));

            SolveResult result = solver.Solve(cube.ToFaceletString());

            AssertSolves(cube, result);
        }

        [Test]
        public void Refining_NeverMakesTheSolutionLonger([Range(0, 2)] int seed)
        {
            var refined = new KociembaSolver(TableFolder, refineSeconds: 1f);
            refined.PrepareAsync().Wait();
            CubeState cube = Scrambled(seed);
            string facelets = cube.ToFaceletString();

            SolveResult plain = solver.Solve(facelets);
            SolveResult better = refined.Solve(facelets);

            AssertSolves(cube, better);
            Assert.LessOrEqual(better.Moves.Count, plain.Moves.Count);
        }
    }
}