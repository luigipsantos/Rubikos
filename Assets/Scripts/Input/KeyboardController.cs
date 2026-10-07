using Rubikos.Core;
using Rubikos.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubikos.Controls
{
    /// <summary>
    /// Keyboard controls. Direct mode: U D L R F B M E S (Shift = counter-clockwise).
    /// Selector mode: 1/2/3 pick an axis, Up/Down pick a layer, Left/Right turn it.
    /// </summary>
    public class KeyboardController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private LayerSelector selector;

        private static readonly (Key Key, char Letter)[] NotationKeys =
        {
            (Key.U, 'U'), (Key.D, 'D'), (Key.L, 'L'), (Key.R, 'R'), (Key.F, 'F'), (Key.B, 'B'),
            (Key.M, 'M'), (Key.E, 'E'), (Key.S, 'S'),
        };

        private GUIStyle helpStyle;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // Ignore Ctrl/Alt combos so we never fight with Unity Editor shortcuts.
            if (kb.ctrlKey.isPressed || kb.altKey.isPressed) return;

            if (kb.backspaceKey.wasPressedThisFrame) game.Undo();

            if (kb.hKey.wasPressedThisFrame) game.ToggleHints();
            if (kb.tKey.wasPressedThisFrame) game.ToggleTraining();
            if (kb.nKey.wasPressedThisFrame) game.PlayNextHint();
            if (kb.pKey.wasPressedThisFrame) game.PlayAllHints();

            HandleNotationKeys(kb);
            HandleSelectorKeys(kb);
        }

        private void HandleNotationKeys(Keyboard kb)
        {
            int amount = kb.shiftKey.isPressed ? -1 : 1;

            foreach (var entry in NotationKeys)
                if (kb[entry.Key].wasPressedThisFrame)
                    game.Submit(MoveNotation.FromNotation(entry.Letter, amount));
        }

        private void HandleSelectorKeys(Keyboard kb)
        {
            if (kb.digit1Key.wasPressedThisFrame) selector.SelectAxis(Axis.X); // columns
            if (kb.digit2Key.wasPressedThisFrame) selector.SelectAxis(Axis.Y); // rows
            if (kb.digit3Key.wasPressedThisFrame) selector.SelectAxis(Axis.Z); // front-to-back slices

            if (kb.upArrowKey.wasPressedThisFrame)   selector.MoveLayer(+1);
            if (kb.downArrowKey.wasPressedThisFrame) selector.MoveLayer(-1);

            if (kb.escapeKey.wasPressedThisFrame) selector.Clear();

            if (!selector.HasSelection) return;

            if (kb.rightArrowKey.wasPressedThisFrame) game.Submit(new Move(selector.Axis, selector.Layer, 1));
            if (kb.leftArrowKey.wasPressedThisFrame)  game.Submit(new Move(selector.Axis, selector.Layer, -1));
        }

        // Temporary help text (a real UI comes in P7).
        private void OnGUI()
        {
            helpStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(12, 10, 1000, 170),
                "Faces: U D L R F B M E S   (Shift = counter-clockwise)\n" +
                "Select: 1 = column, 2 = row, 3 = slice   Up/Down = pick layer   Esc = clear\n" +
                "Turn selected layer: Right / Left arrow     Undo: Backspace\n" +
                "Hints: H   Training: T   Do next hint: N   Play all hints: P\n" +
                "Mouse: left drag on a face = turn that layer, left click = select layer (click again = next axis)\n" +
                "Camera: right mouse drag = orbit, wheel = zoom\n" +
                "Selected: " + selector.Describe(),
                helpStyle);
        }
    }
}