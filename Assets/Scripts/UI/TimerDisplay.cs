using Rubikos.Core;
using Rubikos.Game;
using TMPro;
using UnityEngine;

namespace Rubikos.UI
{
    /// <summary>Shows the solve timer and the best time. It only displays what GameController knows.</summary>
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color finishedColor = new Color(0.3f, 1f, 0.48f);

        private void Update()
        {
            SolveTimer timer = game.Timer;
            double seconds = timer.ElapsedSeconds(Time.realtimeSinceStartupAsDouble);
            string best = game.BestSeconds > 0
                ? "Best: " + SolveTimer.Format(game.BestSeconds)
                : "Best: none yet";

            string time;
            string detail;
            switch (timer.State)
            {
                case SolveTimerState.Armed:
                    time = SolveTimer.Format(0);
                    detail = "The clock starts with your first move.\n" + best;
                    break;

                case SolveTimerState.Running:
                    time = SolveTimer.Format(seconds);
                    detail = (game.HintsUsedThisRun ? "Hints used: this solve is not a record.\n" : string.Empty) + best;
                    break;

                case SolveTimerState.Finished:
                    time = SolveTimer.Format(seconds);
                    if (game.LastSolveWasRecord) detail = "New best time!";
                    else if (game.HintsUsedThisRun) detail = "Solved with hints, so it is not a record.\n" + best;
                    else detail = best;
                    break;

                default:
                    time = "-:--.--";
                    detail = "Click Scramble for a timed solve.\n" + best;
                    break;
            }

            SetIfChanged(timeText, time);
            SetIfChanged(detailText, detail);
            timeText.color = timer.State == SolveTimerState.Finished ? finishedColor : normalColor;
        }

        // Assigning TMP text makes it rebuild its mesh, so only do it when something really changed.
        private static void SetIfChanged(TMP_Text label, string value)
        {
            if (label.text != value) label.text = value;
        }
    }
}