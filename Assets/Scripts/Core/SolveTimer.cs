using System;

namespace Rubikos.Core
{
    public enum SolveTimerState { Idle, Armed, Running, Finished }

    /// <summary>
    /// A stopwatch for timed solves. It never reads a clock itself: you pass in the current time,
    /// which makes it easy to test. Flow: Arm (after a scramble) -> first move starts it -> Stop when solved.
    /// </summary>
    public sealed class SolveTimer
    {
        private double startTime;
        private double finalSeconds;

        public SolveTimerState State { get; private set; } = SolveTimerState.Idle;

        public void Reset()
        {
            State = SolveTimerState.Idle;
            startTime = 0;
            finalSeconds = 0;
        }

        /// <summary>Waiting for the player's first move.</summary>
        public void Arm()
        {
            Reset();
            State = SolveTimerState.Armed;
        }

        /// <summary>Call on every player move. Only the first one after Arm() starts the clock.</summary>
        public void NotifyMove(double now)
        {
            if (State != SolveTimerState.Armed) return;
            startTime = now;
            State = SolveTimerState.Running;
        }

        public void Stop(double now)
        {
            if (State != SolveTimerState.Running) return;
            finalSeconds = now - startTime;
            State = SolveTimerState.Finished;
        }

        public double ElapsedSeconds(double now)
        {
            switch (State)
            {
                case SolveTimerState.Running:  return Math.Max(0, now - startTime);
                case SolveTimerState.Finished: return finalSeconds;
                default:                       return 0;
            }
        }

        /// <summary>Formats as minutes:seconds.hundredths, for example 1:05.25. Truncates, so it never shows a time early.</summary>
        public static string Format(double seconds)
        {
            if (seconds < 0) seconds = 0;

            // The tiny extra amount protects against floating point noise (0.29 * 100 = 28.999999...).
            long hundredths = (long)Math.Floor(seconds * 100.0 + 1e-6);
            long minutes = hundredths / 6000;
            long wholeSeconds = hundredths / 100 % 60;
            long rest = hundredths % 100;
            return $"{minutes}:{wholeSeconds:00}.{rest:00}";
        }
    }
}