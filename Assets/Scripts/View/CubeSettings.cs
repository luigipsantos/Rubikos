using UnityEngine;

namespace Rubikos.View
{
    [CreateAssetMenu(fileName = "CubeSettings", menuName = "Rubikos/Cube Settings")]
    public class CubeSettings : ScriptableObject
    {
        [Tooltip("Seconds one quarter turn takes.")]
        [Min(0f)] public float turnDuration = 0.25f;

        [Tooltip("Speed profile of a turn: slow start, fast middle, slow end.")]
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}