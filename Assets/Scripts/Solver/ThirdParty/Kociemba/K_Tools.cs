using System;
using System.IO;

namespace Kociemba
{
    public class Tools
    {
        // ==================== RUBIKOS CHANGES START ====================
        // The original used BinaryFormatter and a hard-coded Windows path ("Assets\Kociemba\Tables\").
        // Now the folder is configurable, files are written with BinaryWriter, and every file is written
        // under a temporary name first and renamed when complete, so a half-written table can never be loaded.

        /// <summary>Folder that holds the lookup tables. Set it once, before the first solve.</summary>
        public static string TableFolder = "KociembaTables";

        /// <summary>The 12 table files the solver needs (names come from CoordCubeTables).</summary>
        public static readonly string[] TableFiles =
        {
            "twist", "flip", "FRtoBR", "URFtoDLF", "URtoDF", "URtoUL", "UBtoDF", "MergeURtoULandUBtoDF",
            "Slice_URFtoDLF_Parity_Prun", "Slice_URtoDF_Parity_Prun", "Slice_Twist_Prun", "Slice_Flip_Prun"
        };

        public static bool TablesExist()
        {
            foreach (string name in TableFiles)
                if (!File.Exists(PathOf(name))) return false;
            return true;
        }

        /// <summary>Deletes only the table files (not the whole folder), so they get rebuilt next time.</summary>
        public static void DeleteTables()
        {
            foreach (string name in TableFiles)
            {
                string path = PathOf(name);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static string PathOf(string filename) => Path.Combine(TableFolder, filename);

        private static void WriteAtomically(string filename, Action<BinaryWriter> write)
        {
            Directory.CreateDirectory(TableFolder);
            string finalPath = PathOf(filename);
            string tempPath = finalPath + ".tmp";

            using (var writer = new BinaryWriter(File.Open(tempPath, FileMode.Create)))
                write(writer);

            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tempPath, finalPath);
        }

        public static void SerializeTable(string filename, short[,] array)
        {
            WriteAtomically(filename, writer =>
            {
                int rows = array.GetLength(0);
                int cols = array.GetLength(1);
                writer.Write(rows);
                writer.Write(cols);
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        writer.Write(array[r, c]);
            });
        }

        public static short[,] DeserializeTable(string filename)
        {
            using (var reader = new BinaryReader(File.Open(PathOf(filename), FileMode.Open, FileAccess.Read)))
            {
                int rows = reader.ReadInt32();
                int cols = reader.ReadInt32();
                var array = new short[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        array[r, c] = reader.ReadInt16();
                return array;
            }
        }

        public static void SerializeSbyteArray(string filename, sbyte[] array)
        {
            WriteAtomically(filename, writer =>
            {
                writer.Write(array.Length);
                for (int i = 0; i < array.Length; i++)
                    writer.Write(array[i]);
            });
        }

        public static sbyte[] DeserializeSbyteArray(string filename)
        {
            using (var reader = new BinaryReader(File.Open(PathOf(filename), FileMode.Open, FileAccess.Read)))
            {
                int length = reader.ReadInt32();
                var array = new sbyte[length];
                for (int i = 0; i < length; i++)
                    array[i] = reader.ReadSByte();
                return array;
            }
        }
        // ===================== RUBIKOS CHANGES END =====================

        // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Check if the cube string s represents a solvable cube.
        // 0: Cube is solvable
        // -1: There is not exactly one facelet of each colour
        // -2: Not all 12 edges exist exactly once
        // -3: Flip error: One edge has to be flipped
        // -4: Not all corners exist exactly once
        // -5: Twist error: One corner has to be twisted
        // -6: Parity error: Two corners or two edges have to be exchanged
        public static int verify(string s)
        {
            int[] count = new int[6];
            try
            {
                for (int i = 0; i < 54; i++)
                {
                    // RUBIKOS FIX: the original parsed i.ToString() (the loop number) instead of the sticker letter.
                    count[(int)CubeColor.Parse(typeof(CubeColor), s.Substring(i, 1))]++;
                }
            }
            catch (Exception)
            {
                return -1;
            }

            for (int i = 0; i < 6; i++)
            {
                if (count[i] != 9)
                {
                    return -1;
                }
            }

            FaceCube fc = new FaceCube(s);
            CubieCube cc = fc.toCubieCube();
            return cc.verify();
        }

        /// <summary>Generates a random cube. Each cube of the cube space has the same probability.</summary>
        public static string randomCube()
        {
            CubieCube cc = new CubieCube();
            Random gen = new Random();
            cc.setFlip((short)gen.Next(CoordCube.N_FLIP));
            cc.setTwist((short)gen.Next(CoordCube.N_TWIST));
            do
            {
                cc.setURFtoDLB(gen.Next(CoordCube.N_URFtoDLB));
                cc.setURtoBR(gen.Next(CoordCube.N_URtoBR));
            } while ((cc.edgeParity() ^ cc.cornerParity()) != 0);
            FaceCube fc = cc.toFaceCube();
            return fc.to_fc_String();
        }
    }
}