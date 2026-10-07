using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubikos.Controls
{
    /// <summary>
    /// Right mouse button drag = orbit around the cube. Scroll wheel = zoom.
    /// </summary>
    public class CameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 target = Vector3.zero;
        [SerializeField, Tooltip("Degrees of rotation per pixel the mouse moves.")]
        private float rotateSpeed = 0.25f;
        [SerializeField] private float zoomSpeed = 0.01f;
        [SerializeField] private Vector2 distanceRange = new Vector2(5f, 20f);

        private float yaw;
        private float pitch;
        private float distance;

        private void Start()
        {
            // Start from wherever you placed the camera in the Editor so it doesn't jump.
            Vector3 offset = transform.position - target;
            distance = Mathf.Clamp(offset.magnitude, distanceRange.x, distanceRange.y);

            Vector3 angles = Quaternion.LookRotation(-offset).eulerAngles;
            yaw = angles.y;
            pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        }

        private void LateUpdate()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * rotateSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, -85f, 85f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, distanceRange.x, distanceRange.y);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            transform.position = target - rotation * Vector3.forward * distance;
        }
    }
}