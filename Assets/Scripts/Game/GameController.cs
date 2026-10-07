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
    /// The conductor. Owns the rules model, the move history and the hint system.
    /// Model first: the state changes immediately, and the queue animates it afterwards.
    /// </summary>
    public class GameController : MonoBehaviour
    {
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

        public bool IsBusy => moveQueue.IsBusy;
        public int MoveCount => history.Count;
        public bool CanUndo => history.Count > 0;

        /// <summary>Raised whenever the model applies a move (player moves, undo and scramble alike).</summary>
        public event Action<Move> MoveSubmitted;

        /// <summary>Raised when something the UI shows may have changed.</summary>
        public event Action Changed;

        private void Awake()
        {
            // The solver's lookup tables live outside the project, so they are never committed to Git.
            string tableFolder = Path.Combine(Application.persistentDataPath, "KociembaTables");
            Debug.Log("Solver tables folder: " + tableFolder);

            solver = new KociembaSolver(tableFolder);
            Hints = new HintService(solver);
        }

        private void Start()
        {
            _ = solver.PrepareAsync(); // runs in the background: the game stays responsive
        }

        private void OnEnable()  { moveQueue.BecameIdle += OnQueueIdle; }
        private void OnDisable() { moveQueue.BecameIdle -= OnQueueIdle; } // always unsubscribe!

        private void OnDestroy()
        {
            Hints?.Deactivate(); // cancels any solve still running
        }

        /// <summary>A move made by the player: it is recorded so it can be undone.</summary>
        public void Submit(Move move)
        {
            history.Push(move);
            Execute(move, 1f);
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

        /// <summary>Mixes the cube from its current state. The scramble is not undoable and not counted.</summary>
        public void Scramble()
        {
            history.Clear();
            foreach (Move move in Scrambler.Generate(scrambleLength))
                Execute(move, scrambleSpeed);

            Hints.Refresh(State); // one request for the whole scramble, not one per move
            Changed?.Invoke();
        }

        /// <summary>Instantly returns to the solved cube, even in the middle of an animation.</summary>
        public void ResetCube()
        {
            moveQueue.Cancel();       // 1. stop animating
            cubeView.ResetToSolved(); // 2. snap the 3D cubies home
            State.Reset();            // 3. reset the model
            history.Clear();          // 4. forget the history
            Hints.Refresh(State);     // 5. hints (if on) start over

            VerifyView();
            Changed?.Invoke();
        }

        public void ToggleHints()
        {
            if (Hints.IsActive) Hints.Deactivate();
            else _ = Hints.Activate(State);
        }

        // The one place where a move really happens: model first, then animation.
        private void Execute(Move move, float speed)
        {
            State.Apply(move);
            MoveSubmitted?.Invoke(move);
            moveQueue.Enqueue(move, speed);
        }

        private void OnQueueIdle()
        {
            VerifyView();
            Changed?.Invoke();
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