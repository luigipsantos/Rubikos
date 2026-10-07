using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rubikos.Core;
using Rubikos.Solver;

namespace Rubikos.Tests
{
    public class HintServiceTests
    {
        // Pretend solver: instant, no threads. Tests stay fast and predictable.
        private sealed class FakeSolver : ISolver
        {
            public List<Move> Solution = new List<Move>();
            public string FailWith;
            public int Calls;

            public SolverStatus Status => SolverStatus.Ready;
            public string StatusMessage => "Ready";
            public Task PrepareAsync() => Task.CompletedTask;

            public Task<SolveResult> SolveAsync(string facelets, CancellationToken cancellation = default)
            {
                Calls++;
                SolveResult result = FailWith != null
                    ? SolveResult.Fail(FailWith)
                    : SolveResult.Ok(new List<Move>(Solution));
                return Task.FromResult(result);
            }
        }

        // Pretend solver whose answers we release by hand, to test answers that arrive late.
        private sealed class ManualSolver : ISolver
        {
            public readonly List<TaskCompletionSource<SolveResult>> Pending = new List<TaskCompletionSource<SolveResult>>();

            public SolverStatus Status => SolverStatus.Ready;
            public string StatusMessage => "Ready";
            public Task PrepareAsync() => Task.CompletedTask;

            public Task<SolveResult> SolveAsync(string facelets, CancellationToken cancellation = default)
            {
                var source = new TaskCompletionSource<SolveResult>();
                Pending.Add(source);
                return source.Task;
            }
        }

        private static List<Move> Moves(string notation) => MoveNotation.Parse(notation);
        private static Move OneMove(string notation) => MoveNotation.Parse(notation)[0];

        private static (FakeSolver solver, HintService hints) Create(string solution)
        {
            var solver = new FakeSolver { Solution = Moves(solution) };
            return (solver, new HintService(solver));
        }

        [Test]
        public void Activate_StoresTheSolversMoves()
        {
            var (solver, hints) = Create("R U' F2");

            hints.Activate(new CubeState());

            Assert.AreEqual(HintStatus.Ready, hints.Status);
            Assert.AreEqual("R U' F2", MoveNotation.ToNotation(hints.Remaining));
            Assert.AreEqual(1, solver.Calls);
        }

        [Test]
        public void PlayingTheHintedMove_AdvancesWithoutAskingAgain()
        {
            var (solver, hints) = Create("R U' F2");
            var cube = new CubeState();
            hints.Activate(cube);

            hints.NotifyMove(OneMove("R"), cube);

            Assert.AreEqual("U' F2", MoveNotation.ToNotation(hints.Remaining));
            Assert.AreEqual(1, solver.Calls);
        }

        [Test]
        public void PlayingTheLastHintedMove_MarksSolved()
        {
            var (_, hints) = Create("R");
            var cube = new CubeState();
            hints.Activate(cube);

            hints.NotifyMove(OneMove("R"), cube);

            Assert.AreEqual(HintStatus.Solved, hints.Status);
            Assert.AreEqual(0, hints.Remaining.Count);
        }

        [Test]
        public void HalfTurnHinted_QuarterTurnPlayed_LeavesAQuarterTurn()
        {
            var (solver, hints) = Create("R2 U");
            var cube = new CubeState();
            hints.Activate(cube);

            hints.NotifyMove(OneMove("R"), cube);

            Assert.AreEqual("R U", MoveNotation.ToNotation(hints.Remaining));
            Assert.AreEqual(1, solver.Calls);
        }

        [Test]
        public void QuarterTurnHinted_OppositeQuarterPlayed_BecomesAHalfTurn()
        {
            var (_, hints) = Create("R U");
            var cube = new CubeState();
            hints.Activate(cube);

            hints.NotifyMove(OneMove("R'"), cube);

            Assert.AreEqual("R2 U", MoveNotation.ToNotation(hints.Remaining));
        }

        [Test]
        public void PlayingADifferentMove_AsksTheSolverAgain()
        {
            var (solver, hints) = Create("R U");
            var cube = new CubeState();
            hints.Activate(cube);

            hints.NotifyMove(OneMove("F"), cube);

            Assert.AreEqual(2, solver.Calls);
        }

        [Test]
        public void Deactivate_ClearsEverything_AndIgnoresLaterMoves()
        {
            var (solver, hints) = Create("R U");
            var cube = new CubeState();
            hints.Activate(cube);

            hints.Deactivate();
            hints.NotifyMove(OneMove("F"), cube);

            Assert.AreEqual(HintStatus.Off, hints.Status);
            Assert.AreEqual(0, hints.Remaining.Count);
            Assert.AreEqual(1, solver.Calls);
        }

        [Test]
        public void SolverFailure_ShowsFailedWithTheMessage()
        {
            var solver = new FakeSolver { FailWith = "boom" };
            var hints = new HintService(solver);

            hints.Activate(new CubeState());

            Assert.AreEqual(HintStatus.Failed, hints.Status);
            Assert.AreEqual("boom", hints.Error);
        }

        [Test]
        public void OutdatedAnswers_AreIgnored()
        {
            // Unity's editor has a "synchronization context" that would delay continuations to the next frame.
            // We switch it off for this test so everything runs immediately and in order.
            SynchronizationContext previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                var solver = new ManualSolver();
                var hints = new HintService(solver);
                var cube = new CubeState();

                hints.Activate(cube); // request 1
                hints.Refresh(cube); // request 2 makes request 1 outdated

                solver.Pending[1].SetResult(SolveResult.Ok(Moves("R"))); // the NEW answer arrives first
                Assert.AreEqual("R", MoveNotation.ToNotation(hints.Remaining));

                solver.Pending[0].SetResult(SolveResult.Ok(Moves("U F"))); // the OLD answer arrives late
                Assert.AreEqual("R", MoveNotation.ToNotation(hints.Remaining), "the late answer must be ignored");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }
    }
}