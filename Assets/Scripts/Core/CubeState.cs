using System;
using System.Collections.Generic;
using System.Text;

namespace Rubikos.Core
{
    /// <summary>
    /// The whole cube as 54 stickers. No Unity code: this is pure logic.
    /// </summary>
    public sealed class CubeState
    {
        private readonly Sticker[] stickers = new Sticker[54];

        public CubeState() { Reset(); }

        public void Reset()
        {
            int i = 0;
            foreach (Face face in FaceInfo.All)
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                        stickers[i++] = new Sticker(
                            FaceInfo.CellPosition(face, row, col), FaceInfo.Normal(face), face);
        }

        public CubeState Clone()
        {
            var copy = new CubeState();
            Array.Copy(stickers, copy.stickers, stickers.Length); // Sticker is immutable, so a shallow copy is safe
            return copy;
        }

        public void Apply(Move move)
        {
            for (int i = 0; i < stickers.Length; i++)
                if (stickers[i].Position.Get(move.Axis) == move.Layer)
                    stickers[i] = stickers[i].Rotated(move.Axis, move.Turns);
        }

        public void Apply(IEnumerable<Move> moves)
        {
            foreach (Move move in moves) Apply(move);
        }

        /// <summary>
        /// Solved = every face has one single color (whatever the cube's orientation).
        /// </summary>
        public bool IsSolved
        {
            get
            {
                string s = ToFaceletString();
                for (int face = 0; face < 6; face++)
                    for (int k = 1; k < 9; k++)
                        if (s[face * 9 + k] != s[face * 9]) return false;
                return true;
            }
        }

        /// <summary>
        /// The 54-letter string solvers use: faces in order U R F D L B, each read row by row.
        /// Each letter is named after the face whose CENTER has that color right now, so it
        /// still works after middle-slice moves or whole-cube rotations.
        /// </summary>
        public string ToFaceletString()
        {
            // Which face currently holds the center of each color?
            var centerHolder = new Face[6];
            foreach (Sticker s in stickers)
                if (s.Position == s.Normal) // only a center cubie has position == its one normal
                    centerHolder[(int)s.Color] = FaceInfo.FromNormal(s.Normal);

            var sb = new StringBuilder(54);
            foreach (Face face in FaceInfo.All)
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        Sticker s = Find(FaceInfo.CellPosition(face, row, col), FaceInfo.Normal(face));
                        sb.Append(centerHolder[(int)s.Color].ToString());
                    }
            return sb.ToString();
        }

        private Sticker Find(Int3 position, Int3 normal)
        {
            foreach (Sticker s in stickers)
                if (s.Position == position && s.Normal == normal) return s;
            throw new InvalidOperationException("Cube state is corrupted.");
        }
    }
}