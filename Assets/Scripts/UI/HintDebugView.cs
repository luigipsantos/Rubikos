using System.Text;
using Rubikos.Core;
using Rubikos.Game;
using Rubikos.Solver;
using UnityEngine;

namespace Rubikos.UI
{
    /// <summary>TEMPORARY: prints the solver status and the hint list on screen. Replaced in P9.</summary>
    public class HintDebugView : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField, Min(1)] private int maxMovesShown = 12;

        private GUIStyle style;

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                richText = true,
                normal = { textColor = Color.white }
            };

            var text = new StringBuilder();
            text.AppendLine($"Solver: {game.SolverMessage}");

            HintService hints = game.Hints;
            switch (hints.Status)
            {
                case HintStatus.Off:
                    text.Append("Hints: off (press H)");
                    break;
                case HintStatus.Solving:
                    text.Append("Hints: thinking...");
                    break;
                case HintStatus.Solved:
                    text.Append("Hints: nothing left to do");
                    break;
                case HintStatus.Failed:
                    text.Append($"Hints: failed ({hints.Error})");
                    break;
                case HintStatus.Ready:
                    text.Append($"Hints: {hints.Remaining.Count} moves left.  Next: ");
                    for (int i = 0; i < hints.Remaining.Count && i < maxMovesShown; i++)
                    {
                        string notation = MoveNotation.ToNotation(hints.Remaining[i]);
                        text.Append(i == 0 ? $"<color=#4CFF7A><b>{notation}</b></color> " : notation + " ");
                    }
                    if (hints.Remaining.Count > maxMovesShown) text.Append("...");
                    break;
            }

            GUI.Label(new Rect(12, 165, 1400, 120), text.ToString(), style);
        }
    }
}