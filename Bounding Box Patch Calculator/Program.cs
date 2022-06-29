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
            args = new string[] { @"G:\Steam\steamapps\common\ELDEN RING 1.05\Game\parts\wp_a_0617.partsbnd.dcx" };
#endif
            if (args.Length == 0)
            {
                args = Directory.GetFiles(ExeDir, "*.partsbnd*", SearchOption.TopDirectoryOnly);

                if (args.Length == 0)
                {
                    Console.WriteLine("No parts files detected. Drag and drop the parts you want to patch onto this exe, " +
                        "or run this exe in a folder full of parstbnds/flvers (Will not look in nested folders)");
                    Console.ReadLine();
                    return;
                }
            }

            //Console.WriteLine("This program just recalculates the bounding box of each mesh's bones using the Bounding Box Solver from FBX2FLVER.");
            //Console.WriteLine("To use this patcher, move the files you're trying to patch to a new folder and run this exe inside that folder.");
            //Console.WriteLine("Most of the time you DO NOT want to use direct bone indicies, from what I can tell, but I made it an option.");
            Console.WriteLine("This program just recalculates the bounding box of each mesh's bones using the Bounding Box code from:");
            Console.WriteLine("SoulsAssetPipeline by Meowmaritus");
            Console.WriteLine();

            //Console.WriteLine("Use direct bone indicies? [Y/N] (Change this if you don't get desired output)");
            //var useDirectBoneIndicies = YesOrNo();

            //Console.WriteLine("Would you like to use a custom bounding box multiplier? [Y/N] (Default is 2.25)");
            //var useCustomMultiplier = YesOrNo();

            //float multiplier = 2.25f;
            //if (useCustomMultiplier)
            //{
            //    Console.WriteLine("Enter a custom multiplier (eg 1.5)");
            //    var input = Console.ReadLine();
            //    var tryParse = float.TryParse(input, out multiplier);

            //    if (!tryParse)
            //    {
            //        Console.WriteLine("Invalid Multiplier. Using default");
            //        multiplier = 2.25f;
            //    }
            //}

            PatchFiles(args);
            Console.WriteLine("Press the Any Key to Terminate Program");
            Console.ReadKey();
        }

        private static void PatchFiles(string[] parts)
        {
            foreach (var file in parts)
            {
                Console.WriteLine(file);

                if (!File.Exists($"{file}.bak"))
                    File.Copy(file, $"{file}.bak");

                if (file.Contains(".flver"))
                    PatchFlver(file, FLVER2.Read(file));

                if (!file.Contains(".partsbnd"))
                    continue;

                if (BND4.IsRead(file, out BND4 partBND4))
                {
                    PatchParts(partBND4);
                    partBND4.Write(file);
                }
                else if (BND3.IsRead(file, out BND3 partBND3))
                {
                    PatchParts(partBND3);
                    partBND3.Write(file);
                }

            }
        }

        private static void PatchParts(IBinder part)
        {
        

            for (int i = 0; i < part.Files.Count; i++)
            {
                if (part.Files[i].Name.EndsWith(".flver"))
                {
                    var flver = FLVER2.Read(part.Files[i].Bytes);
#if DEBUG
                    PrintDebugInfo(flver);
#endif
                    BoundingBoxSolver.FixAllBoundingBoxes(flver);
#if DEBUG
                    PrintDebugInfo(flver);
#endif
                    part.Files[i].Bytes = flver.Write();
                }
            }
        }

        private static void PatchFlver(string file, FLVER2 flver)
        {
            Console.WriteLine(file);

            if (!File.Exists($"{file}.bak"))
                File.Copy(file, $"{file}.bak");

#if DEBUG
            PrintDebugInfo(flver);
#endif

            BoundingBoxSolver.FixAllBoundingBoxes(flver);
#if DEBUG
            PrintDebugInfo(flver);
#endif
            flver.Write(file);
        }

#if DEBUG
        private static void PrintDebugInfo(FLVER2 flver)
        {
            Console.WriteLine();
            //Console.WriteLine($"{file}");
            Console.WriteLine("Header");
            Console.WriteLine($"BBMin: {flver.Header.BoundingBoxMin}");
            Console.WriteLine($"BBMax: {flver.Header.BoundingBoxMax}");
            Console.WriteLine();
            Console.WriteLine("Bones");
            foreach (FLVER.Bone bone in flver.Bones)
            {
                Console.WriteLine($"{bone.Name}");
                Console.WriteLine($"BBMin: {bone.BoundingBoxMin}");
                Console.WriteLine($"BBMax: {bone.BoundingBoxMax}");
            }

            Console.WriteLine();
            Console.WriteLine("Meshes");
            int num = 0;
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                Console.WriteLine($"{mesh} {num}");
                Console.WriteLine($"BBMin: {mesh.BoundingBox?.Min}");
                Console.WriteLine($"BBMax: {mesh.BoundingBox?.Max}");
                num++;
            }
        }

#endif

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
