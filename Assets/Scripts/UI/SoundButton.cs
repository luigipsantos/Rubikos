using Rubikos.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rubikos.UI
{
    /// <summary>The Sound ON/OFF button.</summary>
    public class SoundButton : MonoBehaviour
    {
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            button.onClick.AddListener(OnClicked);
            audioPlayer.MuteChanged += Refresh;
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClicked);
            audioPlayer.MuteChanged -= Refresh;
        }

        // Start (not OnEnable): AudioPlayer loads the saved setting in its Awake.
        private void Start() => Refresh();

        private void OnClicked()
        {
            audioPlayer.ToggleMute();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Refresh() => label.text = audioPlayer.Muted ? "Sound: OFF" : "Sound: ON";
    }
}