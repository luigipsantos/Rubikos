using System;
using System.Collections;
using System.Collections.Generic;
using Rubikos.Core;
using UnityEngine;

namespace Rubikos.View
{
    /// <summary>
    /// The 3D cube. It only ANIMATES moves and never decides rules.
    /// </summary>
    public class CubeView : MonoBehaviour
    {
        private CubieView[] cubies;
        private Transform pivot;

        private void Awake()
        {
            cubies = GetComponentsInChildren<CubieView>();
            if (cubies.Length != 26)
                Debug.LogError($"CubeView expected 26 cubies but found {cubies.Length}. Rebuild the cube.", this);

            // The pivot is an invisible helper object at the center of the cube.
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(transform, false);
        }

        /// <summary>Plays one turn as a coroutine. A coroutine can pause every frame and continue later.</summary>
        public IEnumerator PlayMove(Move move, float duration, AnimationCurve easing)
        {
            Vector3 axis = AxisVector(move.Axis);

            // 1. Collect the 9 cubies in the layer.
            var layer = new List<CubieView>(9);
            foreach (CubieView cubie in cubies)
                if (AxisValue(cubie.Coord, move.Axis) == move.Layer)
                    layer.Add(cubie);

            // 2. Hang them on the pivot (true = keep their current world position).
            foreach (CubieView cubie in layer)
                cubie.transform.SetParent(pivot, true);

            // 3. Rotate the pivot a little every frame.
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pivot.localRotation = Quaternion.AngleAxis(move.Degrees * easing.Evaluate(t), axis);
                yield return null; // wait for the next frame
            }
            pivot.localRotation = Quaternion.AngleAxis(move.Degrees, axis);

            // 4. Give the cubies back to the cube and snap them to exact values.
            foreach (CubieView cubie in layer)
            {
                cubie.transform.SetParent(transform, true);
                cubie.CommitTurn(move);
            }
            pivot.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Debug check: does every visible sticker show the color the rules model says it should?
        /// This is what proves the model and the view agree.
        /// </summary>
        public bool Matches(CubeState state, out string error)
        {
            foreach (CubieView cubie in cubies)
            {
                foreach (Transform child in cubie.transform)
                {
                    if (!child.gameObject.activeSelf || !child.name.StartsWith("Sticker_")) continue;

                    // The sticker's name tells us its ORIGINAL face, which is its color.
                    Face color = (Face)Enum.Parse(typeof(Face), child.name.Substring("Sticker_".Length));

                    // Where does that sticker point now?
                    Int3 normal = FaceInfo.Normal(color);
                    Vector3 pointing = cubie.transform.localRotation * new Vector3(normal.X, normal.Y, normal.Z);
                    Int3 currentNormal = new Int3(
                        Mathf.RoundToInt(pointing.x), Mathf.RoundToInt(pointing.y), Mathf.RoundToInt(pointing.z));
                    Int3 position = new Int3(cubie.Coord.x, cubie.Coord.y, cubie.Coord.z);

                    if (state.ColorAt(position, currentNormal) != color)
                    {
                        error = $"{cubie.name}: sticker {color} at {position} facing {currentNormal} disagrees with the model.";
                        return false;
                    }
                }
            }
            error = null;
            return true;
        }

        private static Vector3 AxisVector(Axis axis)
            => axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward;

        private static int AxisValue(Vector3Int v, Axis axis)
            => axis == Axis.X ? v.x : axis == Axis.Y ? v.y : v.z;
    }
}