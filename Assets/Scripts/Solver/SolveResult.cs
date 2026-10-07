using System.Collections.Generic;
using Rubikos.Core;

namespace Rubikos.Solver
{
    /// <summary>Either a list of moves (success) or an error message (failure).</summary>
    public readonly struct SolveResult
    {
        private SolveResult(bool success, IReadOnlyList<Move> moves, string error)
        {
            Success = success;
            Moves = moves;
            Error = error;
        }

        public bool Success { get; }
        public IReadOnlyList<Move> Moves { get; }
        public string Error { get; }

        public static SolveResult Ok(IReadOnlyList<Move> moves)
            => new SolveResult(true, moves ?? new List<Move>(), null);

        public static SolveResult Fail(string error)
            => new SolveResult(false, new List<Move>(), error);
    }
}