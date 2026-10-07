using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rubikos.Core;

namespace Rubikos.Solver
{
    public enum HintStatus { Off, Solving, Ready, Solved, Failed }

    /// <summary>
    /// Keeps a "what should I do next" list up to date.
    /// Plain C# (no Unity), so it can be unit-tested with a fake solver.
    /// All methods must be called from the main thread.
    /// </summary>
    public sealed class HintService
    {
        private readonly ISolver solver;
        private readonly List<Move> remaining = new List<Move>();
        private CancellationTokenSource currentRequest;
        private int version; // bumped on every request: lets us ignore answers that arrive too late

        public HintService(ISolver solver)
        {
            this.solver = solver ?? throw new ArgumentNullException(nameof(solver));
        }

        public HintStatus Status { get; private set; } = HintStatus.Off;
        public bool IsActive => Status != HintStatus.Off;
        public IReadOnlyList<Move> Remaining => remaining;
        public string Error { get; private set; }

        /// <summary>Raised whenever Status or Remaining changes.</summary>
        public event Action Changed;

        /// <summary>Turns hints on and asks the solver about this cube.</summary>
        public Task Activate(CubeState state) => RequestAsync(state);

        public void Deactivate()
        {
            CancelRequest();
            version++; // any answer still on its way is now outdated
            remaining.Clear();
            Error = null;
            Status = HintStatus.Off;
            Changed?.Invoke();
        }

        /// <summary>The cube was replaced (scramble, reset): start over, but only if hints are on.</summary>
        public void Refresh(CubeState state)
        {
            if (IsActive) _ = RequestAsync(state);
        }

        /// <summary>Call after every move. Follows the hint when possible, otherwise asks the solver again.</summary>
        public void NotifyMove(Move move, CubeState stateAfterMove)
        {
            if (!IsActive) return;

            if (Status == HintStatus.Ready && TryAdvance(move))
            {
                Status = remaining.Count == 0 ? HintStatus.Solved : HintStatus.Ready;
                Changed?.Invoke();
                return;
            }

            _ = RequestAsync(stateAfterMove);
        }

        // The player turned the layer the hint was about. Subtract what they did from what was suggested:
        // hint R2 + played R -> R left;  hint R + played R' -> R2 left;  hint R + played R -> nothing left.
        private bool TryAdvance(Move played)
        {
            Move next = remaining[0];
            if (next.Axis != played.Axis || next.Layer != played.Layer) return false;

            int left = (((next.Turns - played.Turns) % 4) + 4) % 4;

            remaining.RemoveAt(0);
            if (left != 0) remaining.Insert(0, new Move(next.Axis, next.Layer, left));
            return true;
        }

        /// <summary>
        /// Would this move bring the player closer to the solution?
        /// Training mode uses it to accept only helpful moves. When there is no hint to follow, every move is fine.
        /// </summary>
        public bool IsProgress(Move move)
        {
            if (Status != HintStatus.Ready) return true;

            Move next = remaining[0];
            if (next.Axis != move.Axis || next.Layer != move.Layer) return false;

            int left = Normalize(next.Turns - move.Turns);
            return Cost(left) < Cost(next.Turns);
        }

        private static int Normalize(int turns) => ((turns % 4) + 4) % 4; // always 0, 1, 2 or 3

        // Quarter turns still needed: 0 (done), 1 (one quarter turn either way) or 2 (a half turn).
        private static int Cost(int turns)
        {
            int t = Normalize(turns);
            return t == 0 ? 0 : t == 2 ? 2 : 1;
        }

        private async Task RequestAsync(CubeState state)
        {
            CancelRequest();
            var request = new CancellationTokenSource();
            currentRequest = request;
            int myVersion = ++version;

            // A string is safe to hand to another thread. The live CubeState is not.
            string facelets = state.ToFaceletString();

            remaining.Clear();
            Error = null;
            Status = HintStatus.Solving;
            Changed?.Invoke();

            SolveResult result;
            try
            {
                // In Unity, the code after this "await" continues back on the main thread.
                result = await solver.SolveAsync(facelets, request.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                result = SolveResult.Fail(e.Message);
            }

            if (myVersion != version) return; // the cube changed while we were thinking: ignore this answer

            if (result.Success)
            {
                remaining.AddRange(result.Moves);
                Status = remaining.Count == 0 ? HintStatus.Solved : HintStatus.Ready;
            }
            else
            {
                Error = result.Error;
                Status = HintStatus.Failed;
            }
            Changed?.Invoke();
        }

        private void CancelRequest()
        {
            currentRequest?.Cancel(); // cancelled sources are simply left to the garbage collector
            currentRequest = null;
        }
    }
}