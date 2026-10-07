using Rubikos.Core;
using Rubikos.Game;
using Rubikos.Solver;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rubikos.UI
{
    /// <summary>
    /// Draws a pulsing curved arrow around the layer of the next hinted move.
    /// The arrow slides around the layer so it always stays on the side facing the camera.
    /// It is hidden while the cube is animating.
    /// </summary>
    public class GhostArrowView : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Camera cam;

        [Header("Look")]
        [SerializeField] private Color color = new Color(0.3f, 1f, 0.48f, 1f);
        [SerializeField, Tooltip("Distance of the arrow from the cube's center line.")]
        private float radius = 2.4f;
        [SerializeField] private float lineWidth = 0.14f;
        [SerializeField, Tooltip("Width of the arrow head, as a multiple of the line width.")]
        private float headWidth = 2.6f;
        [SerializeField, Tooltip("Length of the arrow head in world units.")]
        private float headLength = 0.9f;
        [SerializeField, Tooltip("How many degrees of the circle the arrow covers for a quarter turn.")]
        private float quarterTurnSweep = 100f;
        [SerializeField, Tooltip("How many degrees of the circle the arrow covers for a half turn.")]
        private float halfTurnSweep = 190f;

        private const int PointCount = 32;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Vector3[] points = new Vector3[PointCount];
        private LineRenderer line;
        private Material material;
        private Move shownMove;
        private bool hasShownMove;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;

            if (arrowPrefab == null)
            {
                Debug.LogError("GhostArrowView: assign the GhostArrow prefab.", this);
                enabled = false;
                return;
            }

            // Spawn the arrow from the prefab, so it never clutters the Hierarchy at edit time.
            GameObject instance = Instantiate(arrowPrefab);
            instance.name = "GhostArrow (runtime)";

            line = instance.GetComponent<LineRenderer>();
            if (line == null)
            {
                Debug.LogError("The GhostArrow prefab needs a Line Renderer component.", this);
                enabled = false;
                return;
            }

            // Settings that must be right for this script to work (set here so the prefab can't get them wrong).
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View; // the ribbon always faces the camera
            line.positionCount = PointCount;
            line.numCapVertices = 4;
            line.widthMultiplier = lineWidth;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            material = line.material; // our own copy, so we can pulse its color
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (line != null) Destroy(line.gameObject);
        }

        // LateUpdate runs after the camera has moved this frame, so the arrow never lags behind it.
        private void LateUpdate()
        {
            HintService hints = game.Hints;
            bool show = hints != null && hints.Status == HintStatus.Ready && !game.IsBusy;

            if (!show)
            {
                line.enabled = false;
                hasShownMove = false;
                return;
            }

            Move move = hints.Remaining[0];
            if (!hasShownMove || move != shownMove)
            {
                shownMove = move;
                hasShownMove = true;
                BuildWidthCurve(move); // only needed when the move changes
            }

            UpdateShape(move);
            UpdateColor();
            line.enabled = true;
        }

        private void UpdateShape(Move move)
        {
            Vector3 axis = AxisVector(move.Axis);

            // Where is the camera, as seen from the layer's axis? Put the middle of the arrow there.
            Vector3 center = Vector3.ProjectOnPlane(cam.transform.position, axis);
            if (center.sqrMagnitude < 0.01f) // camera looks straight along the axis: any direction will do
                center = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.Cross(axis, Vector3.up);
            center = center.normalized * radius;

            float sweep = SweepFor(move);
            float direction = Mathf.Sign(move.Turns); // +1 or -1: which way around the axis the layer turns

            for (int i = 0; i < PointCount; i++)
            {
                float t = i / (float)(PointCount - 1); // 0 at the tail, 1 at the arrow tip

                // Same rotation call the cube animation uses, so the arrow always points the way the layer turns.
                float angle = Mathf.Lerp(-sweep / 2f, sweep / 2f, t) * direction;
                points[i] = Quaternion.AngleAxis(angle, axis) * center + axis * move.Layer;
            }
            line.SetPositions(points);
        }

        private void UpdateColor()
        {
            // A gentle pulse makes it feel like a ghost, and helps it stand out against the cube.
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 4f);
            Color c = color;
            c.a = color.a * pulse;
            material.SetColor(BaseColorId, c);
        }

        // Thin line that suddenly gets wide (the head) and then tapers to a point at the tip.
        private void BuildWidthCurve(Move move)
        {
            float arcLength = radius * SweepFor(move) * Mathf.Deg2Rad;
            float headFraction = Mathf.Clamp(headLength / arcLength, 0.1f, 0.45f);
            float headStart = 1f - headFraction;

            line.widthCurve = LinearCurve(
                (0f, 1f),
                (headStart, 1f),
                (headStart + 0.03f, headWidth),
                (1f, 0f));
        }

        private float SweepFor(Move move) => Mathf.Abs(move.Turns) == 2 ? halfTurnSweep : quarterTurnSweep;

        private static Vector3 AxisVector(Axis axis)
            => axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward;

        // An AnimationCurve made of straight segments between the given (time, value) points.
        private static AnimationCurve LinearCurve(params (float time, float value)[] pts)
        {
            var keys = new Keyframe[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                float inSlope = i == 0 ? 0f : Slope(pts[i - 1], pts[i]);
                float outSlope = i == pts.Length - 1 ? 0f : Slope(pts[i], pts[i + 1]);
                keys[i] = new Keyframe(pts[i].time, pts[i].value, inSlope, outSlope);
            }
            return new AnimationCurve(keys);
        }

        private static float Slope((float time, float value) a, (float time, float value) b)
            => (b.value - a.value) / (b.time - a.time);
    }
}