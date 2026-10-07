using System.Text;
using Rubikos.Core;
using Rubikos.Game;
using Rubikos.Solver;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rubikos.UI
{
    /// <summary>
    /// The learning panel: next move in big letters, a plain-English description,
    /// the moves after that, and buttons for hints, training and auto-play.
    /// It only shows state and forwards clicks. It contains no cube rules.
    /// </summary>
    public class HintPanel : MonoBehaviour
    {
        [SerializeField] private GameController game;

        [Header("Buttons")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button playAllButton;

        [Header("Button labels that change")]
        [SerializeField] private TMP_Text hintButtonLabel;
        [SerializeField] private TMP_Text trainingButtonLabel;

        [Header("Texts")]
        [SerializeField] private TMP_Text solverStatusText;
        [SerializeField] private TMP_Text nextMoveText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text upcomingText;
        [SerializeField] private TMP_Text messageText;

        [Header("Settings")]
        [SerializeField, Min(1)] private int upcomingShown = 6;
        [SerializeField, Min(0.5f)] private float messageSeconds = 2.5f;

        private float messageHideTime;
        private string shownSolverMessage;

        private void OnEnable()
        {
            hintButton.onClick.AddListener(OnHintClicked);
            trainingButton.onClick.AddListener(OnTrainingClicked);
            nextButton.onClick.AddListener(OnNextClicked);
            playAllButton.onClick.AddListener(OnPlayAllClicked);

            game.Changed += Refresh;
            game.MoveRejected += OnMoveRejected;
        }

        private void OnDisable()
        {
            hintButton.onClick.RemoveListener(OnHintClicked);
            trainingButton.onClick.RemoveListener(OnTrainingClicked);
            nextButton.onClick.RemoveListener(OnNextClicked);
            playAllButton.onClick.RemoveListener(OnPlayAllClicked);

            game.Changed -= Refresh;
            game.MoveRejected -= OnMoveRejected;
        }

        // Start (not OnEnable) because GameController creates the hint system in its Awake,
        // and Unity doesn't guarantee that Awake of another object has run before our OnEnable.
        private void Start()
        {
            messageText.text = string.Empty;
            Refresh();
        }

        private void Update()
        {
            UpdateSolverText();

            if (messageText.text.Length > 0 && Time.time >= messageHideTime)
                messageText.text = string.Empty;
        }

        private void OnHintClicked()     { game.ToggleHints();   ReleaseFocus(); }
        private void OnTrainingClicked() { game.ToggleTraining(); ReleaseFocus(); }
        private void OnNextClicked()     { game.PlayNextHint();   ReleaseFocus(); }
        private void OnPlayAllClicked()  { game.PlayAllHints();   ReleaseFocus(); }

        private void OnMoveRejected(Move rejected)
        {
            HintService hints = game.Hints;
            if (hints.Status != HintStatus.Ready) return;

            string wanted = MoveNotation.ToNotation(hints.Remaining[0]);
            messageText.text = $"Not that move. Try {wanted}.";
            messageHideTime = Time.time + messageSeconds;
        }

        // The solver reports progress from a background thread, so we simply look at it every frame.
        private void UpdateSolverText()
        {
            string message = game.SolverMessage;
            if (message == shownSolverMessage) return;

            shownSolverMessage = message;
            solverStatusText.text = "Hint engine: " + message;
        }

        private void Refresh()
        {
            HintService hints = game.Hints;

            hintButtonLabel.text = hints.IsActive ? "Hints: ON" : "Hints: OFF";
            trainingButtonLabel.text = game.TrainingMode ? "Training: ON" : "Training: OFF";

            bool ready = hints.Status == HintStatus.Ready;
            nextButton.interactable = ready;
            playAllButton.interactable = ready;

            switch (hints.Status)
            {
                case HintStatus.Off:
                    Show("-", "Press Hints to get help.", string.Empty);
                    break;
                case HintStatus.Solving:
                    Show("...", "Thinking...", string.Empty);
                    break;
                case HintStatus.Solved:
                    Show("-", "Nothing left to do.", string.Empty);
                    break;
                case HintStatus.Failed:
                    Show("?", "The hint failed: " + hints.Error, string.Empty);
                    break;
                case HintStatus.Ready:
                    ShowNextMove(hints);
                    break;
            }
        }

        private void ShowNextMove(HintService hints)
        {
            Move next = hints.Remaining[0];

            string description = MoveDescription.Describe(next, game.State);
            if (next.Turns != 2) description += ", looking straight at it";

            var later = new StringBuilder();
            int count = hints.Remaining.Count;
            later.Append(count).Append(count == 1 ? " move left" : " moves left");

            if (count > 1)
            {
                later.Append(". Then: ");
                for (int i = 1; i < count && i <= upcomingShown; i++)
                    later.Append(MoveNotation.ToNotation(hints.Remaining[i])).Append(' ');
                if (count - 1 > upcomingShown) later.Append("...");
            }

            Show(MoveNotation.ToNotation(next), description + ".", later.ToString());
        }

        private void Show(string big, string description, string upcoming)
        {
            nextMoveText.text = big;
            descriptionText.text = description;
            upcomingText.text = upcoming;
        }

        // After a click, a UI button stays "selected" and would react to arrow keys and Enter.
        private static void ReleaseFocus()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}