using System;
using System.Collections;
using System.Collections.Generic;
using Rubikos.Core;
using UnityEngine;

namespace Rubikos.View
{
    /// <summary>Plays animated moves one after another, so animations never overlap.</summary>
    public class MoveQueue : MonoBehaviour
    {
        [SerializeField] private CubeView cubeView;
        [SerializeField] private CubeSettings settings;

        // A queued move plus how much faster than normal it should play.
        private readonly struct Pending
        {
            public readonly Move Move;
            public readonly float SpeedMultiplier;

            public Pending(Move move, float speedMultiplier)
            {
                Move = move;
                SpeedMultiplier = speedMultiplier;
            }
        }

        private readonly Queue<Pending> pending = new Queue<Pending>();
        private bool running;

        public bool IsBusy => running || pending.Count > 0;
        public int PendingCount => pending.Count;

        /// <summary>Raised after each animated move finishes.</summary>
        public event Action<Move> MoveFinished;

        /// <summary>Raised when the last queued move has finished.</summary>
        public event Action BecameIdle;

        /// <param name="speedMultiplier">1 = normal speed, 4 = four times faster.</param>
        public void Enqueue(Move move, float speedMultiplier = 1f)
        {
            pending.Enqueue(new Pending(move, Mathf.Max(0.01f, speedMultiplier)));
            if (!running) StartCoroutine(Run());
        }

        /// <summary>
        /// Stops the current animation and forgets everything still waiting.
        /// The caller must reset the view afterwards (CubeView.ResetToSolved).
        /// </summary>
        public void Cancel()
        {
            StopAllCoroutines();
            pending.Clear();
            running = false;
        }

        private IEnumerator Run()
        {
            running = true;
            while (pending.Count > 0)
            {
                Pending next = pending.Dequeue();
                yield return cubeView.PlayMove(next.Move, settings.turnDuration / next.SpeedMultiplier, settings.easing);
                MoveFinished?.Invoke(next.Move);
            }
            running = false;
            BecameIdle?.Invoke();
        }
    }
}