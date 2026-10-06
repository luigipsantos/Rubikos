using UnityEngine;

namespace Rubikos.View
{
    /// <summary>
    /// One small cube of the Rubik's cube.
    /// Stores its grid coordinate (-1, 0 or 1 on each axis).
    /// </summary>
    public class CubieView : MonoBehaviour
    {
        [SerializeField] private Vector3Int coord;

        public Vector3Int Coord => coord;

        public void Configure(Vector3Int gridCoord)
        {
            coord = gridCoord;

            SetStickerActive("Sticker_U", coord.y == 1);
            SetStickerActive("Sticker_D", coord.y == -1);
            SetStickerActive("Sticker_R", coord.x == 1);
            SetStickerActive("Sticker_L", coord.x == -1);
            SetStickerActive("Sticker_F", coord.z == -1);
            SetStickerActive("Sticker_B", coord.z == 1);
        }

        private void SetStickerActive(string stickerName, bool active)
        {
            Transform sticker = transform.Find(stickerName);
            if (sticker != null) sticker.gameObject.SetActive(active);
        }
    }
}