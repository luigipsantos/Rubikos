using Rubikos.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubikos.Game
{
    /// <summary>
    /// TEMPORARY: lets us test animations. Deleted in P5.
    /// </summary>
    public class TemporaryDebugInput : MonoBehaviour
    {
        [SerializeField] private GameController game;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            string suffix = kb.shiftKey.isPressed ? "'" : "";

            if (kb.rKey.wasPressedThisFrame) game.Submit(MoveNotation.Parse("R" + suffix));
            if (kb.uKey.wasPressedThisFrame) game.Submit(MoveNotation.Parse("U" + suffix));
            if (kb.fKey.wasPressedThisFrame) game.Submit(MoveNotation.Parse("F" + suffix));
            if (kb.mKey.wasPressedThisFrame) game.Submit(MoveNotation.Parse("M" + suffix));

            if (kb.spaceKey.wasPressedThisFrame)  game.Submit(MoveNotation.Parse("R U R' U'"));
            if (kb.digit6Key.wasPressedThisFrame)
                game.Submit(MoveNotation.Parse("R U R' U' R U R' U' R U R' U' R U R' U' R U R' U' R U R' U'"));
        }
    }
}