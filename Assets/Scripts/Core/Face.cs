namespace Rubikos.Core
{
    /// <summary>The order U R F D L B is the order the solver will expect later.</summary>
    public enum Face { U, R, F, D, L, B }

    /// <summary>
    /// For each face: the direction it points (Normal) and how its 3x3 grid is laid out
    /// when you look at it from outside (Right = screen right, Down = screen down).
    /// This matches the standard "unfolded cube" layout used by solvers.
    /// </summary>
    public static class FaceInfo
    {
        public static readonly Face[] All = { Face.U, Face.R, Face.F, Face.D, Face.L, Face.B };

        //                                    U                 R                 F                  D                  L                  B
        private static readonly Int3[] Normals = { new Int3(0,1,0),  new Int3(1,0,0),  new Int3(0,0,-1), new Int3(0,-1,0), new Int3(-1,0,0), new Int3(0,0,1)  };
        private static readonly Int3[] Rights  = { new Int3(1,0,0),  new Int3(0,0,1),  new Int3(1,0,0),  new Int3(1,0,0),  new Int3(0,0,-1), new Int3(-1,0,0) };
        private static readonly Int3[] Downs   = { new Int3(0,0,-1), new Int3(0,-1,0), new Int3(0,-1,0), new Int3(0,0,1),  new Int3(0,-1,0), new Int3(0,-1,0) };

        public static Int3 Normal(Face face) => Normals[(int)face];

        /// <summary>Grid coordinate of the cubie shown at (row, col) of a face. Row and col are 0..2.</summary>
        public static Int3 CellPosition(Face face, int row, int col)
            => Normals[(int)face] + Rights[(int)face] * (col - 1) + Downs[(int)face] * (row - 1);

        public static Face FromNormal(Int3 normal)
        {
            foreach (Face face in All)
                if (Normals[(int)face] == normal) return face;
            throw new System.ArgumentException($"{normal} is not a face normal.");
        }
    }
}