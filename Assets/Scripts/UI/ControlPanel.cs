using Rubikos.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rubikos.UI
{
    /// <summary>
    /// Buttons (Scramble, Undo, Reset) plus the move counter and the "Solved!" message.
    /// It only shows state and forwards clicks. It contains no cube rules.
    /// </summary>
    public class ControlPanel : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private Button scrambleButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text moveCounterText;
        [SerializeField] private TMP_Text statusText;

        private void OnEnable()
        {
            scrambleButton.onClick.AddListener(OnScramble);
            undoButton.onClick.AddListener(OnUndo);
            resetButton.onClick.AddListener(OnReset);
            game.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            scrambleButton.onClick.RemoveListener(OnScramble);
            undoButton.onClick.RemoveListener(OnUndo);
            resetButton.onClick.RemoveListener(OnReset);
            game.Changed -= Refresh;
        }

        private void OnScramble() { game.Scramble();  ReleaseFocus(); }
        private void OnUndo()     { game.Undo();      ReleaseFocus(); }
        private void OnReset()    { game.ResetCube(); ReleaseFocus(); }

        private void Refresh()
        {
            moveCounterText.text = $"Moves: {game.MoveCount}";
            undoButton.interactable = game.CanUndo;

            // "Solved!" only after the animation has finished and only if the player actually moved.
            bool solved = !game.IsBusy && game.State.IsSolved && game.MoveCount > 0;
            statusText.text = solved ? "Solved!" : string.Empty;
        }

        // After a click, a UI button stays "selected" and would react to arrow keys and Enter.
        // Letting go of the selection keeps the keyboard controls for the cube.
        private static void ReleaseFocus()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}