using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Rubikos.Core;

namespace Rubikos.Solver
{
    /// <summary>
    /// Wraps the Kociemba library behind ISolver.
    /// Everything slow runs on a background thread, and a lock makes sure only one solve runs at a time
    /// (the library keeps its working data in static arrays, so it is not thread-safe).
    /// </summary>
    public sealed class KociembaSolver : ISolver
    {
        // A cube with a single R turn applied. The answer must be "R'", which proves the tables loaded
        // AND that our facelet string and notation agree with the library.
        private const string WarmUpFacelets =
            "UUFUUFUUF" + "RRRRRRRRR" + "FFDFFDFFD" + "DDBDDBDDB" + "LLLLLLLLL" + "UBBUBBUBB";

        private const int MaxDepth = 22;

        // This port multiplies the timeout by 1024 internally (seen in K_Search.cs), so 5 means about 5 seconds.
        private const long TimeoutUnits = 5;

        private static readonly object SolverLock = new object();

        private readonly float refineSeconds;
        private readonly object prepareGate = new object();
        private Task prepareTask;
        private volatile SolverStatus status = SolverStatus.NotStarted;
        private volatile string statusMessage = "Not started";

        /// <param name="tableFolder">Where the lookup tables are stored (built once, reused after).</param>
        /// <param name="refineSeconds">After the first answer, spend up to this long looking for a shorter one.</param>
        public KociembaSolver(string tableFolder, float refineSeconds = 2f)
        {
            this.refineSeconds = Math.Max(0f, refineSeconds);
            Kociemba.Tools.TableFolder = tableFolder;
        }

        public SolverStatus Status => status;
        public string StatusMessage => statusMessage;

        public Task PrepareAsync()
        {
            lock (prepareGate)
            {
                return prepareTask ??= Task.Run(Prepare);
            }
        }

        public Task<SolveResult> SolveAsync(string facelets, CancellationToken cancellation = default)
        {
            return Task.Run(() =>
            {
                PrepareAsync().Wait(); // we are on a worker thread, so waiting here is fine
                return Solve(facelets, cancellation);
            }, cancellation);
        }

        /// <summary>The synchronous solve. Call it from a background thread (or from tests).</summary>
        public SolveResult Solve(string facelets, CancellationToken cancellation = default)
        {
            if (status != SolverStatus.Ready)
                return SolveResult.Fail("Solver is not ready: " + statusMessage);

            lock (SolverLock)
            {
                try
                {
                    List<Move> best = RunOnce(facelets, MaxDepth, TimeoutUnits, out string error);
                    if (best == null) return SolveResult.Fail(error);

                    // Refinement: keep asking for something one move shorter until the time is up
                    // or the library can't find anything shorter.
                    DateTime deadline = DateTime.UtcNow.AddSeconds(refineSeconds);
                    while (best.Count > 1 && DateTime.UtcNow < deadline && !cancellation.IsCancellationRequested)
                    {
                        List<Move> shorter = RunOnce(facelets, best.Count - 1, 1, out _);
                        if (shorter == null || shorter.Count >= best.Count) break;
                        best = shorter;
                    }
                    return SolveResult.Ok(best);
                }
                catch (Exception e)
                {
                    return SolveResult.Fail(e.Message);
                }
            }
        }

        private void Prepare()
        {
            lock (SolverLock)
            {
                status = SolverStatus.Preparing;
                bool hadTables = Kociemba.Tools.TablesExist();

                try
                {
                    string info;
                    if (!hadTables)
                    {
                        statusMessage = "Building solver tables (first run only, up to a couple of minutes)...";
                        Kociemba.SearchRunTime.solution(WarmUpFacelets, out info, MaxDepth, TimeoutUnits, false, true);
                    }

                    statusMessage = "Loading solver tables...";
                    string answer = Kociemba.Search.solution(WarmUpFacelets, out info, MaxDepth, TimeoutUnits);

                    if (answer == null || answer.Trim() != "R'")
                        throw new InvalidOperationException($"Self-test failed: expected \"R'\" but got \"{answer}\".");

                    status = SolverStatus.Ready;
                    statusMessage = "Ready";
                }
                catch (Exception e)
                {
                    status = SolverStatus.Failed;
                    statusMessage = "Failed: " + (e.InnerException ?? e).Message;

                    // Damaged tables? Delete them so the next Play session rebuilds them.
                    // (The library can't retry inside the same session, because its tables load once.)
                    if (hadTables)
                    {
                        try
                        {
                            Kociemba.Tools.DeleteTables();
                            statusMessage += " (tables deleted, press Play again to rebuild them)";
                        }
                        catch (Exception) { /* nothing more we can do */ }
                    }
                }
            }
        }

        private static List<Move> RunOnce(string facelets, int maxDepth, long timeout, out string error)
        {
            error = null;
            string solution = Kociemba.Search.solution(facelets, out string info, maxDepth, timeout);
            solution = (solution ?? string.Empty).Trim();

            if (solution.StartsWith("Error"))
            {
                error = DescribeError(solution);
                return null;
            }

            try
            {
                return MoveNotation.Parse(CleanSolution(solution));
            }
            catch (FormatException e)
            {
                error = $"Could not read the solver's answer \"{solution}\": {e.Message}";
                return null;
            }
        }

        // The library may add extras like "(21f)" or " . " separators. Our parser wants plain moves only.
        private static string CleanSolution(string solution)
            => Regex.Replace(solution, @"\(.*?\)", " ").Replace(".", " ");

        private static string DescribeError(string code)
        {
            switch (code.Trim())
            {
                case "Error 1": return "The cube does not have exactly 9 stickers of each color.";
                case "Error 2": return "Some edge pieces are missing or duplicated.";
                case "Error 3": return "One edge piece is flipped (impossible cube).";
                case "Error 4": return "Some corner pieces are missing or duplicated.";
                case "Error 5": return "One corner piece is twisted (impossible cube).";
                case "Error 6": return "Two pieces are swapped (impossible cube).";
                case "Error 7": return "No solution within the move limit.";
                case "Error 8": return "The solver ran out of time.";
                default: return code;
            }
        }
    }
}