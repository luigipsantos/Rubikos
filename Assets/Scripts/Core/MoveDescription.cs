namespace Rubikos.Core
{
    /// <summary>
    /// Turns a move into a sentence a beginner understands.
    /// Faces are named by the color of their CENTER piece, because centers never move
    /// (so "the RED face" is always the face with the red center, wherever the camera is).
    /// </summary>
    public static class MoveDescription
    {
        // Same order as the Face enum: U R F D L B (each face's color when the cube is solved).
        private static readonly string[] ColorNames = { "WHITE", "RED", "GREEN", "YELLOW", "ORANGE", "BLUE" };

        public static string Describe(Move move, CubeState state)
        {
            string notation = MoveNotation.ToNotation(move);
            char letter = notation[0];

            string how = notation.EndsWith("2") ? "twice (half turn)"
                       : notation.EndsWith("'") ? "counter-clockwise"
                       : "clockwise";

            switch (letter)
            {
                // Middle slices follow the direction of a neighbouring face: M like L, E like D, S like F.
                case 'M': return $"Turn the middle slice (M) {how}, as seen from the {ColorOf(Face.L, state)} face";
                case 'E': return $"Turn the middle slice (E) {how}, as seen from the {ColorOf(Face.D, state)} face";
                case 'S': return $"Turn the middle slice (S) {how}, as seen from the {ColorOf(Face.F, state)} face";
                default:  return $"Turn the {ColorOf(FaceOfLetter(letter), state)} face {how}";
            }
        }

        // The color of the center piece currently sitting on that face position.
        private static string ColorOf(Face position, CubeState state)
        {
            Int3 normal = FaceInfo.Normal(position);
            return ColorNames[(int)state.ColorAt(normal, normal)];
        }

        private static Face FaceOfLetter(char letter)
        {
            switch (letter)
            {
                case 'U': return Face.U;
                case 'R': return Face.R;
                case 'F': return Face.F;
                case 'D': return Face.D;
                case 'L': return Face.L;
                default:  return Face.B;
            }
        }
    }
}