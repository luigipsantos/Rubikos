using System;

namespace Rubikos.Core
{
    /// <summary>
    /// Integer 3D vector, Immutable.
    /// </summary>
    public readonly struct Int3 : IEquatable<Int3>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Int3(int x, int y, int z) { X = x; Y = y; Z = z; }

        public int Get(Axis axis) => axis == Axis.X ? X : axis == Axis.Y ? Y : Z;

        /// <summary>
        /// Rotates by 90° steps around an axis, using Unity's convention:
        /// a positive turn is clockwise when you look at the origin from the positive end of the axis.
        /// </summary>
        public Int3 Rotated(Axis axis, int quarterTurns)
        {
            int n = ((quarterTurns % 4) + 4) % 4;
            Int3 result = this;
            for (int i = 0; i < n; i++) result = result.RotatedOnce(axis);
            return result;
        }

        private Int3 RotatedOnce(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return new Int3(X, -Z, Y);
                case Axis.Y: return new Int3(Z, Y, -X);
                default:     return new Int3(-Y, X, Z);
            }
        }

        public static Int3 operator +(Int3 a, Int3 b) => new Int3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Int3 operator *(Int3 a, int k) => new Int3(a.X * k, a.Y * k, a.Z * k);
        public static bool operator ==(Int3 a, Int3 b) => a.Equals(b);
        public static bool operator !=(Int3 a, Int3 b) => !a.Equals(b);

        public bool Equals(Int3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Int3 other && Equals(other);
        public override int GetHashCode() => (X + 1) * 9 + (Y + 1) * 3 + (Z + 1);
        public override string ToString() => $"({X},{Y},{Z})";
    }
}