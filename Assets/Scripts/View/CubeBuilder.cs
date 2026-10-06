using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rubikos.View
{
    /// <summary>
    /// Editor helper: fills this object with the 26 visible cubies of a 3x3x3 cube
    /// </summary>
    public class CubeBuilder : MonoBehaviour
    {
        [SerializeField] private GameObject cubiePrefab;

#if UNITY_EDITOR
        [ContextMenu("Build Cube")]
        private void BuildCube()
        {
            if (cubiePrefab == null)
            {
                Debug.LogError("CubeBuilder: assign the Cubie prefab first.", this);
                return;
            }

            ClearChildren();

            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && y == 0 && z == 0) continue; // the hidden core (just ignore it)
                CreateCubie(new Vector3Int(x, y, z));
            }
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }

        private void CreateCubie(Vector3Int coord)
        {
            var cubie = (GameObject)PrefabUtility.InstantiatePrefab(cubiePrefab, transform);
            Undo.RegisterCreatedObjectUndo(cubie, "Build Cube");

            cubie.name = $"Cubie ({coord.x},{coord.y},{coord.z})";
            cubie.transform.localPosition = (Vector3)coord;
            cubie.transform.localRotation = Quaternion.identity;

            var view = cubie.GetComponent<CubieView>();
            view.Configure(coord);

            // Script-made changes on a prefab instance must be registered to be saved
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            foreach (Transform child in cubie.transform)
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
        }
#endif
    }
}