using Rubikos.Core;
using UnityEngine;

namespace Rubikos.View
{
    /// <summary>
    /// One small cube of the Rubik's cube.
    /// Stores its grid coordinate (-1, 0 or 1 on each axis) and keeps it exact after every turn.
    /// </summary>
    public class CubieView : MonoBehaviour
    {
        [SerializeField] private Vector3Int coord;

        public Vector3Int Coord => coord;

        public void Configure(Vector3Int gridCoord)
        {
            coord = gridCoord;

            // A sticker only exists where this cubie touches the OUTSIDE of the big cube.
            // Remember: F faces -Z and B faces +Z.
            SetStickerActive("Sticker_U", coord.y == 1);
            SetStickerActive("Sticker_D", coord.y == -1);
            SetStickerActive("Sticker_R", coord.x == 1);
            SetStickerActive("Sticker_L", coord.x == -1);
            SetStickerActive("Sticker_F", coord.z == -1);
            SetStickerActive("Sticker_B", coord.z == 1);
        }

        /// <summary>
        /// Called when an animated turn has finished and this cubie is back under the cube.
        /// Uses the SAME math as the rules model, then snaps the transform to exact values.
        /// </summary>
        public void CommitTurn(Move move)
        {
            Int3 rotated = new Int3(coord.x, coord.y, coord.z).Rotated(move.Axis, move.Turns);
            coord = new Vector3Int(rotated.X, rotated.Y, rotated.Z);

            transform.localPosition = (Vector3)coord;
            transform.localRotation = SnapToRightAngle(transform.localRotation);
        }

        /// <summary>Rounds a rotation so its axes point exactly along X, Y or Z.</summary>
        private static Quaternion SnapToRightAngle(Quaternion rotation)
        {
            Vector3 forward = Round(rotation * Vector3.forward);
            Vector3 up = Round(rotation * Vector3.up);
            return Quaternion.LookRotation(forward, up);
        }

        private static Vector3 Round(Vector3 v)
            => new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));

        private void SetStickerActive(string stickerName, bool active)
        {
            Transform sticker = transform.Find(stickerName);
            if (sticker != null) sticker.gameObject.SetActive(active);
        }
    }
}