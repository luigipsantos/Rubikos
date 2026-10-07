using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rubikos.UI
{
    /// <summary>
    /// The help and credits screen. Shown the first time the game starts, then opened with the Help button or the I key.
    /// This script must live on an object that stays active (the Canvas), NOT on the overlay it shows and hides.
    /// </summary>
    public class InfoPanel : MonoBehaviour
    {
        private const string SeenKey = "Rubikos.HelpSeen";

        [SerializeField] private GameObject overlay;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("Optional.")] private TMP_Text versionText;

        private void OnEnable()
        {
            openButton.onClick.AddListener(Toggle);
            closeButton.onClick.AddListener(Toggle);
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Toggle);
            closeButton.onClick.RemoveListener(Toggle);
        }

        private void Start()
        {
            if (versionText != null) versionText.text = "Version " + Application.version;

            // First launch: show the help. After that it stays closed until the player asks for it.
            overlay.SetActive(PlayerPrefs.GetInt(SeenKey, 0) == 0);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.iKey.wasPressedThisFrame) Toggle();
        }

        private void Toggle()
        {
            bool open = !overlay.activeSelf;
            overlay.SetActive(open);

            if (!open)
            {
                PlayerPrefs.SetInt(SeenKey, 1); // remember that the player has seen it
                PlayerPrefs.Save();
            }

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}