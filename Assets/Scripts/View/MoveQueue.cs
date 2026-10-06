using System;
using System.Collections;
using System.Collections.Generic;
using Rubikos.Core;
using UnityEngine;

namespace Rubikos.View
{
    /// <summary>
    /// Plays animated moves one after another, so animations never overlap.</summary>
    public class MoveQueue : MonoBehaviour
    {
        [SerializeField] private CubeView cubeView;
        [SerializeField] private CubeSettings settings;

        private readonly Queue<Move> pending = new Queue<Move>();
        private bool running;

        public bool IsBusy => running || pending.Count > 0;
        public int PendingCount => pending.Count;

        /// <summary>
        /// Raised after each animated move finishes.
        /// </summary>
        public event Action<Move> MoveFinished;

        /// <summary>
        /// Raised when the last queued move has finished.
        /// </summary>
        public event Action BecameIdle;

        public void Enqueue(Move move)
        {
            pending.Enqueue(move);
            if (!running) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            running = true;
            while (pending.Count > 0)
            {
                Move move = pending.Dequeue();
                yield return cubeView.PlayMove(move, settings.turnDuration, settings.easing);
                MoveFinished?.Invoke(move);
            }
            running = false;
            BecameIdle?.Invoke();
        }
    }
}