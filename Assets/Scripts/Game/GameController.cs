using System;
using System.Collections.Generic;
using Rubikos.Core;
using Rubikos.View;
using UnityEngine;

namespace Rubikos.Game
{
    /// <summary>
    /// The conductor. Owns the rules model. Model first: the state changes immediately,
    /// and the queue animates it afterwards.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private CubeView cubeView;
        [SerializeField] private MoveQueue moveQueue;

        public CubeState State { get; } = new CubeState();

        /// <summary>
        /// Raised the moment a move is accepted (the hint system and UI will listen).
        /// </summary>
        public event Action<Move> MoveSubmitted;

        private void OnEnable()  { moveQueue.BecameIdle += OnQueueIdle; }
        private void OnDisable() { moveQueue.BecameIdle -= OnQueueIdle; }

        public void Submit(Move move)
        {
            State.Apply(move);
            MoveSubmitted?.Invoke(move);
            moveQueue.Enqueue(move);
        }

        public void Submit(IEnumerable<Move> moves)
        {
            foreach (Move move in moves) Submit(move);
        }

        private void OnQueueIdle()
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