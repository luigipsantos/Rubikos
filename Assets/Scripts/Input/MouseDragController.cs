using Rubikos.Core;
using Rubikos.Game;
using Rubikos.View;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Rubikos.Controls
{
    /// <summary>
    /// Left mouse button:
    ///   drag on a cube face = turn the layer you grab, like a real cube
    ///   click on a cube     = select a layer (click the same cubie again to cycle the axis)
    ///   click on empty space = clear the selection
    /// The right button is used by CameraOrbit, so the two never conflict.
    /// </summary>
    public class MouseDragController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private LayerSelector selector;
        [SerializeField] private Camera cam;
        [SerializeField, Tooltip("Pixels the mouse must travel before it counts as a drag instead of a click.")]
        private float dragThresholdPixels = 15f;

        private enum PressState { Idle, Pressed, Consumed }

        private PressState state;
        private Vector2 pressScreen;
        private CubieView pressedCubie;
        private CubieView lastClickedCubie;
        private Vector3 pressNormal; // which way the grabbed face points (snapped to an axis)
        private Vector3 pressPoint; // where on the cube you grabbed it

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cam == null) return;

            Vector2 position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
                BeginPress(position);

            if (state == PressState.Pressed && mouse.leftButton.isPressed)
            {
                if ((position - pressScreen).magnitude >= dragThresholdPixels)
                    TryDrag(position);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (state == PressState.Pressed) Click();
                state = PressState.Idle;
            }
        }

        private void BeginPress(Vector2 position)
        {
            state = PressState.Idle;

            // Don't grab the cube when the click is really on a UI button.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (game.IsBusy) return;

            if (Physics.Raycast(cam.ScreenPointToRay(position), out RaycastHit hit))
            {
                var cubie = hit.collider.GetComponentInParent<CubieView>();
                if (cubie != null)
                {
                    state = PressState.Pressed;
                    pressScreen = position;
                    pressedCubie = cubie;
                    pressPoint = hit.point;
                    pressNormal = SnapToAxis(hit.normal);
                    return;
                }
            }

            // Pressed on empty space.
            selector.Clear();
            lastClickedCubie = null;
        }

        /// <summary>
        /// The idea: the grabbed face has two directions you could slide along.
        /// Project both onto the screen and pick the one closest to the way the mouse moved.
        /// The layer that turns is the one perpendicular to both the face and that direction.
        /// </summary>
        private void TryDrag(Vector2 position)
        {
            Vector2 drag = (position - pressScreen).normalized;
            Vector2 origin = cam.WorldToScreenPoint(pressPoint);

            Vector3 slideDirection = Vector3.zero;
            float bestScore = 0f;

            foreach (Vector3 candidate in new[] { Vector3.right, Vector3.up, Vector3.forward })
            {
                // Skip the axis the face points along, you can't slide "into" the face.
                if (Mathf.Abs(Vector3.Dot(candidate, pressNormal)) > 0.5f) continue;

                Vector2 onScreen = (Vector2)cam.WorldToScreenPoint(pressPoint + candidate) - origin;
                if (onScreen.sqrMagnitude < 0.0001f) continue;

                float alignment = Vector2.Dot(drag, onScreen.normalized);
                if (Mathf.Abs(alignment) > bestScore)
                {
                    bestScore = Mathf.Abs(alignment);
                    slideDirection = candidate * Mathf.Sign(alignment);
                }
            }

            if (slideDirection == Vector3.zero) return;

            // The layer axis is perpendicular to both the face normal and the slide direction.
            Axis axis = AxisOf(Vector3.Cross(pressNormal, slideDirection));
            Vector3 axisVector = AxisVector(axis);

            // Which way must we spin around that axis so the grabbed face moves along slideDirection?
            // Rotate the face normal by +90 degrees: if it lands along the slide direction, turns = +1.
            float side = Vector3.Dot(Quaternion.AngleAxis(90f, axisVector) * pressNormal, slideDirection);
            int turns = side > 0f ? 1 : -1;

            int layer = CoordOnAxis(pressedCubie.Coord, axis);

            selector.Select(axis, layer); // show what is turning
            game.Submit(new Move(axis, layer, turns));
            state = PressState.Consumed; // one move per drag
        }

        private void Click()
        {
            Axis axis = AxisOf(pressNormal);

            // Clicking the same cubie again cycles X -> Y -> Z.
            if (selector.HasSelection && pressedCubie == lastClickedCubie)
                axis = (Axis)(((int)selector.Axis + 1) % 3);

            selector.Select(axis, CoordOnAxis(pressedCubie.Coord, axis));
            lastClickedCubie = pressedCubie;
        }

        // ---- helpers ----

        private static Vector3 SnapToAxis(Vector3 v)
        {
            float x = Mathf.Abs(v.x), y = Mathf.Abs(v.y), z = Mathf.Abs(v.z);
            if (x >= y && x >= z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (y >= z)           return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static Axis AxisOf(Vector3 v)
            => Mathf.Abs(v.x) > 0.5f ? Axis.X : Mathf.Abs(v.y) > 0.5f ? Axis.Y : Axis.Z;

        private static Vector3 AxisVector(Axis axis)
            => axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward;

        private static int CoordOnAxis(Vector3Int coord, Axis axis)
            => axis == Axis.X ? coord.x : axis == Axis.Y ? coord.y : coord.z;
    }
}