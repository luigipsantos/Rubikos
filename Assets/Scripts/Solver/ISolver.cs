using System.Threading;
using System.Threading.Tasks;

namespace Rubikos.Solver
{
    public enum SolverStatus { NotStarted, Preparing, Ready, Failed }

    /// <summary>
    /// What the game needs from ANY solver. The rest of the project only knows this interface,
    /// so a different solver can be plugged in later without touching anything else.
    /// </summary>
    public interface ISolver
    {
        SolverStatus Status { get; }

        /// <summary>Human-readable progress ("Building tables...", "Ready").</summary>
        string StatusMessage { get; }

        /// <summary>Starts preparing in the background. Safe to call many times.</summary>
        Task PrepareAsync();

        /// <param name="facelets">The 54-letter cube string from CubeState.ToFaceletString().</param>
        Task<SolveResult> SolveAsync(string facelets, CancellationToken cancellation = default);
    }
}