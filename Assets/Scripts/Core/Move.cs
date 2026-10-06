using System;

namespace Rubikos.Core
{
    /// <summary>
    /// "Turn this layer of this axis". Layer is -1, 0 or 1.
    /// Turns uses Unity's rotation direction: 1 = +90°, 2 = 180°, -1 = -90°.
    /// Never write turns by hand: MoveNotation converts from R, U', F2...
    /// </summary>
    public readonly struct Move : IEquatable<Move>
    {
        public Axis Axis { get; }
        public int Layer { get; }
        public int Turns { get; }

        public Move(Axis axis, int layer, int turns)
        {
            if (layer < -1 || layer > 1) throw new ArgumentOutOfRangeException(nameof(layer));

            int t = ((turns % 4) + 4) % 4; // 0..3
            if (t == 0) throw new ArgumentException("A move must turn the layer.", nameof(turns));

            Axis = axis;
            Layer = layer;
            Turns = t == 3 ? -1 : t; // only -1, 1 or 2 are stored
        }

        public Move Inverse => new Move(Axis, Layer, -Turns);
        public float Degrees => Turns * 90f;

        public bool Equals(Move other) => Axis == other.Axis && Layer == other.Layer && Turns == other.Turns;
        public override bool Equals(object obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => ((int)Axis * 3 + (Layer + 1)) * 4 + (Turns + 1);
        public static bool operator ==(Move a, Move b) => a.Equals(b);
        public static bool operator !=(Move a, Move b) => !a.Equals(b);

        public override string ToString() => MoveNotation.ToNotation(this);
    }
}