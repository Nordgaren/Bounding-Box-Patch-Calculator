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
            ExeDir = $@"Path\To\Test\Folder";
#endif
            Console.WriteLine("This program just recalculates the bounding box of each mesh's bones using the Bounding Box Solver from FBX2FLVER.");
            Console.WriteLine("To use this patcher, move the files you're trying to patch to a new folder and run this exe inside that folder.");
            Console.WriteLine("Most of the time you DO NOT want to use direct bone indicies, from what I can tell, but I made it an option.");
            Console.WriteLine();

            var useDirectBoneIndicies = UseDirectBoneIndicies();

            var parts = Directory.GetFiles(ExeDir, "*partsbnd");

            if (parts.Count() == 0)
                parts = Directory.GetFiles(ExeDir, "*partsbnd.dcx");

            if (parts.Count() == 0)
            {
                Console.WriteLine("No parts files detected. Put this EXE in a folder with the parts you want to patch (and only those parts)");
                Console.ReadLine();
                return;
            }

            PatchFiles(useDirectBoneIndicies, parts);
        }

        private static void PatchFiles(bool useDirectBoneIndicies, string[] parts)
        {
            var boundingBoxSolver = new BoundingBoxSolver(useDirectBoneIndicies);

            foreach (var file in parts)
            {
                if (!File.Exists($"{file}.bak"))
                    File.Copy(file, $"{file}.bak");

                Console.WriteLine(file);
                BND3 ogPartBND3;
                BND4 ogPartBND4;
                if (BND3.Is(file))
                {
                    ogPartBND3 = BND3.Read(file);
                    PatchParts(boundingBoxSolver, file, ogPartBND3);
                    ogPartBND3.Write(file);
                }
                else if (BND4.Is(file))
                {
                    ogPartBND4 = BND4.Read(file);
                    PatchParts(boundingBoxSolver, file, ogPartBND4);
                    ogPartBND4.Write(file);
                }

            }
        }

        private static void PatchParts(BoundingBoxSolver boundingBoxSolver, string file, IBinder ogPart)
        {
            foreach (var item in ogPart.Files)
            {
                if (item.Name.EndsWith(".flver"))
                {
                    var ogFLVERBytes = FLVER2.Read(item.Bytes);

                    boundingBoxSolver.FixAllBoundingBoxes(ogFLVERBytes);

                    ogPart.Files[1].Bytes = ogFLVERBytes.Write();
                }
            }
        }

        private static bool UseDirectBoneIndicies()
        {
            Console.WriteLine("Use direct bone indicies? [Y/N] (Change this if you don't get desired output)");
            var result = Console.ReadKey();
            Console.WriteLine();
            if (result.Key == ConsoleKey.Y)
                return true;
            else if (result.Key == ConsoleKey.N)
                return false;
            else
                return UseDirectBoneIndicies();
        }
    }
}
