using System;
using System.Collections.Generic;
using Rubikos.Core;
using Rubikos.View;
using UnityEngine;

namespace Rubikos.Game
{
    /// <summary>
    /// The conductor. Owns the rules model and the move history.
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

        public CubeState State { get; } = new CubeState();
        public bool IsBusy => moveQueue.IsBusy;
        public int MoveCount => history.Count;
        public bool CanUndo => history.Count > 0;

        /// <summary>Raised whenever the model applies a move (player moves, undo and scramble alike).</summary>
        public event Action<Move> MoveSubmitted;

        /// <summary>Raised when something the UI shows may have changed.</summary>
        public event Action Changed;

        private void OnEnable()  { moveQueue.BecameIdle += OnQueueIdle; }
        private void OnDisable() { moveQueue.BecameIdle -= OnQueueIdle; } // always unsubscribe!

        /// <summary>A move made by the player: it is recorded so it can be undone.</summary>
        public void Submit(Move move)
        {
            history.Push(move);
            Execute(move, 1f);
            Changed?.Invoke();
        }

        public void Submit(IEnumerable<Move> moves)
        {
            foreach (Move move in moves) Submit(move);
        }

        public void Undo()
        {
            if (history.Count == 0) return;

            Move last = history.Pop();
            Execute(last.Inverse, 1f);
            Changed?.Invoke();
        }

        /// <summary>Mixes the cube from its current state. The scramble is not undoable and not counted.</summary>
        public void Scramble()
        {
            history.Clear();
            foreach (Move move in Scrambler.Generate(scrambleLength))
                Execute(move, scrambleSpeed);
            Changed?.Invoke();
        }

        /// <summary>Instantly returns to the solved cube, even in the middle of an animation.</summary>
        public void ResetCube()
        {
            moveQueue.Cancel();       // 1. stop animating
            cubeView.ResetToSolved(); // 2. snap the 3D cubies home
            State.Reset();            // 3. reset the model
            history.Clear();          // 4. forget the history

            VerifyView();
            Changed?.Invoke();
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