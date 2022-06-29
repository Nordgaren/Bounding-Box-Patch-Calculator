using System.Collections.Generic;
using HKX2;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace Bounding_Box_Patch_Calculator
{
    public static class BoundingBoxSolver
    {
        public static void FixAllBoundingBoxes(FLVER2 flver)
        {
            flver.Header.BoundingBoxMin = new System.Numerics.Vector3();
            flver.Header.BoundingBoxMax = new System.Numerics.Vector3();
            foreach (FLVER.Bone bone in flver.Bones)
            {
                bone.BoundingBoxMin = new System.Numerics.Vector3();
                bone.BoundingBoxMax = new System.Numerics.Vector3();
            }

            for (int i = 0; i < flver.Meshes.Count; i++)
            {

                FLVER2.Mesh mesh = flver.Meshes[i];
                mesh.BoundingBox = new FLVER2.Mesh.BoundingBoxes();

                foreach (FLVER.Vertex vertex in mesh.Vertices)
                {
                    flver.Header.UpdateBoundingBox(vertex.Position);
                    if (mesh.BoundingBox != null)
                        mesh.UpdateBoundingBox(vertex.Position);

                    for (int j = 0; j < vertex.BoneIndices.Length; j++)
                    {
                        var boneIndex = vertex.BoneIndices[j];
                        var boneDoesNotExist = false;

                        // Mark bone as not-dummied-out since there is geometry skinned to it.
                        if (boneIndex >= 0 && boneIndex < flver.Bones.Count)
                        {
                            flver.Bones[boneIndex].Unk3C = 0;
                        }
                        else
                        {
                            //Logger.LogWarning($"Vertex skinned to bone '{bone.Name}' which does NOT exist in the skeleton.");
                            boneDoesNotExist = true;
                        }

                        if (!boneDoesNotExist)
                            flver.Bones[boneIndex].UpdateBoundingBox(flver.Bones, vertex.Position);
                    }
                }

            }
        }
    }
}