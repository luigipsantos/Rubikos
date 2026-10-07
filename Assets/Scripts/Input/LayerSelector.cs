using System;
using Rubikos.Core;
using UnityEngine;

namespace Rubikos.Controls
{
    /// <summary>
    /// Remembers which layer is selected (an axis plus a layer from -1 to 1)
    /// and shows it with a translucent box spawned from a prefab at startup.
    /// It does not read any input itself.
    /// </summary>
    public class LayerSelector : MonoBehaviour
    {
        [SerializeField] private GameObject highlightPrefab;

        private Transform highlight;

        public bool HasSelection { get; private set; }
        public Axis Axis { get; private set; } = Axis.Y;
        public int Layer { get; private set; } = 1;

        /// <summary>Raised whenever the selection changes.</summary>
        public event Action Changed;

        private void Awake()
        {
            if (highlightPrefab == null)
            {
                Debug.LogError("LayerSelector: assign the SelectionHighlight prefab.", this);
                return;
            }
            highlight = Instantiate(highlightPrefab).transform;
            highlight.name = "SelectionHighlight (runtime)";
        }

        private void Start() => Refresh();

        public void Select(Axis axis, int layer)
        {
            Axis = axis;
            Layer = Mathf.Clamp(layer, -1, 1);
            HasSelection = true;
            Refresh();
        }

        /// <summary>Switch axis but keep the same layer number.</summary>
        public void SelectAxis(Axis axis) => Select(axis, Layer);

        /// <summary>Move the selection by one layer (+1 or -1). It stops at the edges.</summary>
        public void MoveLayer(int step) => Select(Axis, Layer + step);

        public void Clear()
        {
            HasSelection = false;
            Refresh();
        }

        /// <summary>Human-readable name of the selection, for on-screen text.</summary>
        public string Describe()
        {
            if (!HasSelection) return "none";

            string[] names;
            switch (Axis)
            {
                case Axis.X: names = new[] { "Left column", "Middle column", "Right column" }; break;
                case Axis.Y: names = new[] { "Bottom row", "Middle row", "Top row" }; break;
                default:     names = new[] { "Front slice", "Middle slice", "Back slice" }; break;
            }
            return names[Layer + 1];
        }

        private void Refresh()
        {
            if (highlight != null)
            {
                highlight.gameObject.SetActive(HasSelection);
                if (HasSelection)
                {
                    Vector3 direction = Axis == Axis.X ? Vector3.right
                                      : Axis == Axis.Y ? Vector3.up
                                      : Vector3.forward;

                    highlight.position = direction * Layer;
                    // 3.1 on the two other axes, 1.1 along the layer axis.
                    highlight.localScale = Vector3.one * 3.1f - direction * 2f;
                }
            }
            Changed?.Invoke();
        }
    }
}