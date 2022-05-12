using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Bounding_Box_Patch_Calculator
{
    class Program
    {
        public static string ExeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { @"G:\Steam\steamapps\common\ELDEN RING 1.04.2\Game\parts\wp_a_0454-partsbnd-dcx\GR\data\INTERROOT_win64\parts\Weapon\WP_A_9935\WP_A_9935.flver" };
#endif
            if (args.Length == 0)
            {
                args = Directory.GetFiles(ExeDir, "*.partsbnd*");

                if (args.Length == 0)
                {
                    Console.WriteLine("No parts files detected. Drag and drop the parts you want to patch onto this exe, " +
                        "or run this exe in a folder full of parstbnds");
                    Console.ReadLine();
                    return;
                }
            }

            Console.WriteLine("This program just recalculates the bounding box of each mesh's bones using the Bounding Box Solver from FBX2FLVER.");
            Console.WriteLine("To use this patcher, move the files you're trying to patch to a new folder and run this exe inside that folder.");
            Console.WriteLine("Most of the time you DO NOT want to use direct bone indicies, from what I can tell, but I made it an option.");
            Console.WriteLine();

            Console.WriteLine("Use direct bone indicies? [Y/N] (Change this if you don't get desired output)");
            var useDirectBoneIndicies = YesOrNo();

            Console.WriteLine("Would you like to use a custom bounding box multiplier? [Y/N] (Default is 2.25)");
            var useCustomMultiplier = YesOrNo();

            float multiplier = 2.25f;
            if (useCustomMultiplier)
            {
                Console.WriteLine("Enter a custom multiplier (eg 1.5)");
                var input = Console.ReadLine();
                var tryParse = float.TryParse(input, out multiplier);

                if (!tryParse)
                {
                    Console.WriteLine("Invalid Multiplier. Using default");
                    multiplier = 2.25f;
                }
            }

            PatchFiles(useDirectBoneIndicies, args, multiplier);
        }

        private static void PatchFiles(bool useDirectBoneIndicies, string[] parts, float multiplier)
        {
            var boundingBoxSolver = new BoundingBoxSolver(useDirectBoneIndicies);
            boundingBoxSolver.SetMuliplier(multiplier);

            foreach (var file in parts)
            {
                if (file.Contains(".flver"))
                    PatchFlver(boundingBoxSolver, file, FLVER2.Read(file));

                if (!file.Contains(".partsbnd"))
                    continue;

                if (!File.Exists($"{file}.bak"))
                    File.Copy(file, $"{file}.bak");

                Console.WriteLine(file);
                BND3 ogPartBND3;
                BND4 ogPartBND4;

                if (BND4.Is(file))
                {
                    ogPartBND4 = BND4.Read(file);
                    PatchParts(boundingBoxSolver, file, ogPartBND4);
                    ogPartBND4.Write(file);
                }
                else if (BND3.Is(file))
                {
                    ogPartBND3 = BND3.Read(file);
                    PatchParts(boundingBoxSolver, file, ogPartBND3);
                    ogPartBND3.Write(file);
                }

            }
        }

        private static void PatchParts(BoundingBoxSolver boundingBoxSolver, string file, IBinder ogPart)
        {
            for (int i = 0; i < ogPart.Files.Count; i++)
            {
                if (ogPart.Files[i].Name.EndsWith(".flver"))
                {
                    var ogFLVERBytes = FLVER2.Read(ogPart.Files[i].Bytes);

                    boundingBoxSolver.FixAllBoundingBoxes(ogFLVERBytes);

                    ogPart.Files[i].Bytes = ogFLVERBytes.Write();
                }
            }
        }

        private static void PatchFlver(BoundingBoxSolver boundingBoxSolver, string file, FLVER2 ogFlver)
        {
            if (!File.Exists($"{file}.bak"))
                File.Copy(file, $"{file}.bak");

#if DEBUG
            PrintDebugInfo(file, ogFlver);
#endif

            boundingBoxSolver.FixAllBoundingBoxes(ogFlver);
#if DEBUG
            PrintDebugInfo(file, ogFlver);
#endif
            ogFlver.Write(file);
        }

        private static void PrintDebugInfo(string file, FLVER2 ogFlver)
        {
            Console.WriteLine();
            Console.WriteLine($"{file}");
            Console.WriteLine("Header");
            Console.WriteLine($"BBMin: {ogFlver.Header.BoundingBoxMin}");
            Console.WriteLine($"BBMax: {ogFlver.Header.BoundingBoxMax}");
            Console.WriteLine();
            Console.WriteLine("Bones");
            foreach (FLVER.Bone bone in ogFlver.Bones)
            {
                Console.WriteLine($"{bone.Name}");
                Console.WriteLine($"BBMin: {bone.BoundingBoxMin}");
                Console.WriteLine($"BBMax: {bone.BoundingBoxMax}");
            }

            Console.WriteLine();
            Console.WriteLine("Meshes");
            int num = 0;
            foreach (FLVER2.Mesh mesh in ogFlver.Meshes)
            {
                Console.WriteLine($"{mesh} {num}");
                Console.WriteLine($"BBMin: {mesh.BoundingBox?.Min}");
                Console.WriteLine($"BBMax: {mesh.BoundingBox?.Max}");
                num++;
            }
        }


        private static bool YesOrNo()
        {
            var result = Console.ReadKey();
            Console.WriteLine();
            if (result.Key == ConsoleKey.Y)
                return true;
            else if (result.Key == ConsoleKey.N)
                return false;
            else
                return YesOrNo();
        }
    }
}
