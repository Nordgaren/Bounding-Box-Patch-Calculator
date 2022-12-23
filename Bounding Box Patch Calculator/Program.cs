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
            args = new string[] { @"C:\Users\Nord\Downloads\aeg040_097.geombnd.dcx.bak" };
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
            
            if (OodleHandle != IntPtr.Zero)
                Kernel32.FreeLibrary(OodleHandle);
            
            Console.WriteLine("Press the Any Key to Terminate Program");
            Console.ReadKey();
        }

        static IntPtr OodleHandle = IntPtr.Zero;
        
        private static void PatchFiles(string[] parts)
        {
            foreach (var file in parts)
            {
                Console.WriteLine(file);

                if (!File.Exists($"{file}.bak"))
                    File.Copy(file, $"{file}.bak");
                
                TryPatchBND4(file);
                
                if (BND3.IsRead(file, out BND3 bnd3))
                {
                    PatchParts(bnd3);
                    bnd3.Write(file);
                }

                if (FLVER2.IsRead(file, out FLVER2 flver))
                {
                    PatchFlver(flver);
                    flver.Write(file);
                }

            }
        }
        private static void TryPatchBND4(string file)
        {
            try
            {
                if (BND4.IsRead(file, out BND4 bnd4))
                {
                    PatchParts(bnd4);
                    bnd4.Write(file);
                }
            }
            catch (DllNotFoundException ex) when (ex.Message.Contains("oo2core_6_win64.dll"))
            {
                string oo2corePath = Util.GetOodlePath();
                if (oo2corePath == null)
                    throw;

                OodleHandle = Kernel32.LoadLibrary(oo2corePath);
                if (BND4.IsRead(file, out BND4 bnd4))
                {
                    PatchParts(bnd4);
                    bnd4.Write(file);
                }

            }
        }

        private static void PatchParts(IBinder part)
        {

            foreach (BinderFile file in part.Files)
            {

                if (FLVER2.IsRead(file.Bytes, out FLVER2 flver))
                {
                    PatchFlver(flver);
                }
            }
        }

        private static void PatchFlver(FLVER2 flver)
        {
#if DEBUG
            PrintDebugInfo(flver);
#endif
            BoundingBoxSolver.FixAllBoundingBoxes(flver);
#if DEBUG
            PrintDebugInfo(flver);
#endif
        }

#if DEBUG
        private static void PrintDebugInfo(FLVER2 flver)
        {
            Console.WriteLine();
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
