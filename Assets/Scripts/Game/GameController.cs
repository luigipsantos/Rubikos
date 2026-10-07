using System;
using System.Collections.Generic;
using System.IO;
using Rubikos.Core;
using Rubikos.Solver;
using Rubikos.View;
using UnityEngine;

namespace Rubikos.Game
{
    /// <summary>
    /// The conductor. Owns the rules model, the move history, the hint system and the timed-solve session.
    /// Model first: the state changes immediately, and the queue animates it afterwards.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private const string BestTimeKey = "Rubikos.BestSeconds";

        [SerializeField] private CubeView cubeView;
        [SerializeField] private MoveQueue moveQueue;

        [SerializeField, Min(1)] private int scrambleLength = 20;
        [SerializeField, Min(1f), Tooltip("How many times faster than normal the scramble animates.")]
        private float scrambleSpeed = 4f;

        // The player's moves, newest on top. Undo pops from here.
        private readonly Stack<Move> history = new Stack<Move>();

        private ISolver solver;

        public CubeState State { get; } = new CubeState();
        public HintService Hints { get; private set; }
        public SolverStatus SolverStatus => solver.Status;
        public string SolverMessage => solver.StatusMessage;

        /// <summary>In training mode only the suggested move is accepted.</summary>
        public bool TrainingMode { get; private set; }

        // ---- timed solve session ----
        public SolveTimer Timer { get; } = new SolveTimer();

        /// <summary>Best time in seconds without hints. 0 means no record yet.</summary>
        public double BestSeconds { get; private set; }

        /// <summary>True if hints or training were switched on at any point since the last scramble.</summary>
        public bool HintsUsedThisRun { get; private set; }

        public bool LastSolveWasRecord { get; private set; }

        public bool IsBusy => moveQueue.IsBusy;
        public int MoveCount => history.Count;
        public bool CanUndo => history.Count > 0;

        /// <summary>Raised whenever the model applies a move (player moves, undo and scramble alike).</summary>
        public event Action<Move> MoveSubmitted;

        /// <summary>Raised when training mode refuses a move.</summary>
        public event Action<Move> MoveRejected;

        /// <summary>Raised when the player finishes a timed solve (after the last animation).</summary>
        public event Action Solved;

        /// <summary>Raised when something the UI shows may have changed (including hint changes).</summary>
        public event Action Changed;

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private void Awake()
        {
            // The solver's lookup tables live outside the project, so they are never committed to Git.
            string tableFolder = Path.Combine(Application.persistentDataPath, "KociembaTables");
            Debug.Log("Solver tables folder: " + tableFolder);

            solver = new KociembaSolver(tableFolder);
            Hints = new HintService(solver);
            Hints.Changed += OnHintsChanged; // forward, so UI scripts only need to listen to our Changed

            BestSeconds = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        }

        private void Start()
        {
            _ = solver.PrepareAsync(); // runs in the background: the game stays responsive
        }

        private void OnEnable()  { moveQueue.BecameIdle += OnQueueIdle; }
        private void OnDisable() { moveQueue.BecameIdle -= OnQueueIdle; } // always unsubscribe!

        private void OnDestroy()
        {
            if (Hints == null) return;
            Hints.Changed -= OnHintsChanged;
            Hints.Deactivate(); // cancels any solve still running
        }

        /// <summary>A move made by the player: it is recorded so it can be undone.</summary>
        public void Submit(Move move)
        {
            // Training mode: refuse anything that doesn't follow the hint.
            if (TrainingMode && !Hints.IsProgress(move))
            {
                MoveRejected?.Invoke(move);
                return;
            }

            history.Push(move);
            Execute(move, 1f);
            Timer.NotifyMove(Now);
            Hints.NotifyMove(move, State);
            Changed?.Invoke();
        }

        public void Submit(IEnumerable<Move> moves)
        {
            foreach (Move move in moves) Submit(move);
        }

        public void Undo()
        {
            if (history.Count == 0) return;

            Move inverse = history.Pop().Inverse;
            Execute(inverse, 1f);
            Hints.NotifyMove(inverse, State);
            Changed?.Invoke();
        }

        /// <summary>Mixes the cube from its current state and arms the timer. The scramble is not undoable and not counted.</summary>
        public void Scramble()
        {
            history.Clear();
            foreach (Move move in Scrambler.Generate(scrambleLength))
                Execute(move, scrambleSpeed);

            Timer.Arm();
            HintsUsedThisRun = Hints.IsActive;
            LastSolveWasRecord = false;

            Hints.Refresh(State); // one request for the whole scramble, not one per move
            Changed?.Invoke();
        }

        /// <summary>Instantly returns to the solved cube, even in the middle of an animation.</summary>
        public void ResetCube()
        {
            moveQueue.Cancel();       // Stop animating
            cubeView.ResetToSolved(); // Snap the 3D cubies home
            State.Reset();            // Reset the model
            history.Clear();          // Forget the history
            Timer.Reset();            // No timed solve any more
            HintsUsedThisRun = false;
            LastSolveWasRecord = false;
            Hints.Refresh(State);     // Hints (if on) start over

            VerifyView();
            Changed?.Invoke();
        }

        public void ToggleHints()
        {
            if (Hints.IsActive)
            {
                TrainingMode = false; // training needs hints
                Hints.Deactivate();
            }
            else
            {
                HintsUsedThisRun = true;
                _ = Hints.Activate(State);
            }
            Changed?.Invoke();
        }

        public void ToggleTraining()
        {
            TrainingMode = !TrainingMode;
            if (TrainingMode)
            {
                HintsUsedThisRun = true;
                if (!Hints.IsActive) _ = Hints.Activate(State);
            }
            Changed?.Invoke();
        }

        /// <summary>Lets the computer make the next suggested move.</summary>
        public void PlayNextHint()
        {
            if (Hints.Status != HintStatus.Ready) return;
            Submit(Hints.Remaining[0]);
        }

        /// <summary>Lets the computer make every remaining suggested move, one after another.</summary>
        public void PlayAllHints()
        {
            if (Hints.Status != HintStatus.Ready) return;

            // Work on a copy: the hint list shrinks while we submit.
            foreach (Move move in new List<Move>(Hints.Remaining))
                Submit(move);
        }

        // The one place where a move really happens: model first, then animation.
        private void Execute(Move move, float speed)
        {
            State.Apply(move);
            MoveSubmitted?.Invoke(move);
            moveQueue.Enqueue(move, speed);
        }

        private void OnHintsChanged() => Changed?.Invoke();

        private void OnQueueIdle()
        {
            VerifyView();

            // Solved while the clock was running: the cube can only be solved at the end of a timed run,
            // because Undo never goes further back than the (unsolved) scramble.
            if (Timer.State == SolveTimerState.Running && State.IsSolved)
                FinishSolve();

            Changed?.Invoke();
        }

        private void FinishSolve()
        {
            Timer.Stop(Now);
            double seconds = Timer.ElapsedSeconds(Now); // after Stop: the frozen final time

            LastSolveWasRecord = !HintsUsedThisRun && (BestSeconds <= 0 || seconds < BestSeconds);
            if (LastSolveWasRecord)
            {
                BestSeconds = seconds;
                PlayerPrefs.SetFloat(BestTimeKey, (float)seconds);
                PlayerPrefs.Save();
            }

            Solved?.Invoke();
        }

        private void VerifyView()
        {
#if UNITY_EDITOR
            if (cubeView.Matches(State, out string error))
                Debug.Log($"View matches model. Solved: {State.IsSolved}");
            else
                Debug.LogError("VIEW AND MODEL DISAGREE: " + error);
#endif
        }
    }
}