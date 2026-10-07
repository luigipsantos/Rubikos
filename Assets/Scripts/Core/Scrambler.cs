using System;
using System.Collections.Generic;

namespace Rubikos.Core
{
    /// <summary>
    /// Makes random scrambles. Only outer faces (U D L R F B) are used,
    /// and the same axis is never turned twice in a row (so no wasted moves like U D U).
    /// Pure C#: pass a seeded Random to get the same scramble every time (useful in tests).
    /// </summary>
    public static class Scrambler
    {
        private static readonly char[] FaceLetters = { 'U', 'D', 'R', 'L', 'F', 'B' };
        private static readonly int[] Amounts = { 1, -1, 2 }; // clockwise, counter-clockwise, half turn

        public static List<Move> Generate(int length, Random random)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var moves = new List<Move>(length);
            Axis? previousAxis = null;

            while (moves.Count < length)
            {
                char letter = FaceLetters[random.Next(FaceLetters.Length)];
                int amount = Amounts[random.Next(Amounts.Length)];
                Move move = MoveNotation.FromNotation(letter, amount);

                if (move.Axis == previousAxis) continue; // same axis as last time: pick again

                moves.Add(move);
                previousAxis = move.Axis;
            }
            return moves;
        }

        public static List<Move> Generate(int length) => Generate(length, new Random());
    }
}