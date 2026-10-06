using System;
using System.Collections.Generic;
using System.Text;

namespace Rubikos.Core
{
    /// <summary>
    /// Standard cube notation: U D L R F B turn a face clockwise as seen from outside.
    /// ' = counter-clockwise, 2 = half turn. M E S are the middle slices.
    /// </summary>
    public static class MoveNotation
    {
        // Letter, axis, layer, and Sign = the Unity direction of a clockwise turn (+1 or -1).
        private static readonly (char Name, Axis Axis, int Layer, int Sign)[] Table =
        {
            ('U', Axis.Y,  1,  1), ('D', Axis.Y, -1, -1),
            ('R', Axis.X,  1,  1), ('L', Axis.X, -1, -1),
            ('F', Axis.Z, -1, -1), ('B', Axis.Z,  1,  1),
            ('M', Axis.X,  0, -1), ('E', Axis.Y,  0, -1), ('S', Axis.Z,  0, -1),
        };

        /// <param name="amount">1 = clockwise, 2 = half turn, -1 = counter-clockwise</param>
        public static Move FromNotation(char name, int amount)
        {
            foreach (var entry in Table)
                if (entry.Name == name)
                    return new Move(entry.Axis, entry.Layer, amount == 2 ? 2 : amount * entry.Sign);

            throw new FormatException($"Unknown move letter '{name}'.");
        }

        public static string ToNotation(Move move)
        {
            foreach (var entry in Table)
            {
                if (entry.Axis != move.Axis || entry.Layer != move.Layer) continue;

                int amount = move.Turns == 2 ? 2 : move.Turns * entry.Sign;
                return entry.Name + (amount == 2 ? "2" : amount == -1 ? "'" : "");
            }
            throw new ArgumentException("No notation for this move.");
        }

        public static string ToNotation(IEnumerable<Move> moves)
        {
            var sb = new StringBuilder();
            foreach (Move move in moves)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(ToNotation(move));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses text like "R U2 F' M". Also accepts R1 / R3 style used by some solvers.
        /// </summary>
        public static List<Move> Parse(string text)
        {
            var moves = new List<Move>();
            if (string.IsNullOrWhiteSpace(text)) return moves;

            foreach (string token in text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length > 2) throw new FormatException($"Bad move '{token}'.");

                int amount = 1;
                if (token.Length == 2)
                {
                    switch (token[1])
                    {
                        case '\'':
                        case '3': amount = -1; break;
                        case '2': amount = 2; break;
                        case '1': amount = 1; break;
                        default: throw new FormatException($"Bad move '{token}'.");
                    }
                }
                moves.Add(FromNotation(token[0], amount));
            }
            return moves;
        }
    }
}