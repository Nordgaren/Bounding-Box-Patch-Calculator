using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using SharpDX;


namespace Bounding_Box_Patch_Calculator
{
    //This code is from FBX2FLVER. Thank Meowmaritus. 
    public class BoundingBoxSolver
    {
        //private readonly FBX2FLVERImporter Importer;
        //public BoundingBoxSolver(FBX2FLVERImporter Importer)
        //{
        //    this.Importer = Importer;
        //}

        private bool UseDirectBoneIndices = false;

        public BoundingBoxSolver(bool useDirectBoneIndices)
        {
            UseDirectBoneIndices = useDirectBoneIndices;
        }

        private Dictionary<FLVER.Vertex, List<FLVER.Bone>> PrecalculatedBoneLists = new Dictionary<FLVER.Vertex, List<FLVER.Bone>>();

        private List<FLVER.Bone> GetAllBonesReferencedByVertex(FLVER2 f, FLVER2.Mesh m, FLVER.Vertex v)
        {
            if (!PrecalculatedBoneLists.ContainsKey(v))
            {
                List<FLVER.Bone> result = new List<FLVER.Bone>();

                foreach (var vbi in v.BoneIndices)
                {
                    var vertBoneIndex = vbi;

                    if (vertBoneIndex >= 0)
                    {
                        if (m.Dynamic == 0)
                            vertBoneIndex = v.NormalW;

                        if (vertBoneIndex >= f.Bones.Count() || (vertBoneIndex >= m.BoneIndices.Count() && vertBoneIndex >= v.BoneIndices.Count()))
                            continue;

                        if (UseDirectBoneIndices)
                        {
                            result.Add(f.Bones[vertBoneIndex]);
                        }
                        else
                        {
                            if (m.BoneIndices.Count() > 0 && m.BoneIndices[vertBoneIndex] >= 0)
                                result.Add(f.Bones[m.BoneIndices[vertBoneIndex]]);
                            else if (v.BoneIndices.Count() > 0 && v.BoneIndices[vertBoneIndex] >= 0)
                                result.Add(f.Bones[vertBoneIndex]);
                        }
                    }
                }

                PrecalculatedBoneLists.Add(v, result);
            }

            return PrecalculatedBoneLists[v];
        }

        private List<FLVER.Vertex> GetVerticesParentedToBone(FLVER2 f, FLVER.Bone b)
        {
            var result = new List<FLVER.Vertex>();
            foreach (var sm in f.Meshes)
            {
                foreach (var v in sm.Vertices)
                {
                    var bonesReferencedByThisShit = GetAllBonesReferencedByVertex(f, sm, v);
                    if (bonesReferencedByThisShit.Contains(b))
                        result.Add(v);
                }
            }
            return result;
        }

        private BoundingBox GetBoundingBox(Vector3[] verts)
        {
            if (verts.Length > 0)
                return BoundingBox.FromPoints(verts);
            else
                return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        Matrix GetParentBoneMatrix(FLVER2 f, FLVER.Bone bone)
        {
            FLVER.Bone parent = bone;

            var boneParentMatrix = Matrix.Identity;

            do
            {
                boneParentMatrix *= Matrix.Scaling(parent.Scale.X, parent.Scale.Y, parent.Scale.Z);
                boneParentMatrix *= Matrix.Scaling(parent.Rotation.X);
                boneParentMatrix *= Matrix.RotationZ(parent.Rotation.Z);
                boneParentMatrix *= Matrix.RotationY(parent.Rotation.Y);

                //boneParentMatrix *= Matrix.CreateRotationZ(parent.EulerRadian.Z);
                //boneParentMatrix *= Matrix.CreateRotationX(parent.EulerRadian.X);
                //boneParentMatrix *= Matrix.CreateRotationY(parent.EulerRadian.Y);
                boneParentMatrix *= Matrix.Translation(parent.Translation.X, parent.Translation.Y, parent.Translation.Z);
                //boneParentMatrix *= Matrix.CreateScale(parent.Scale);

                if (parent.ParentIndex >= 0)
                {
                    parent = f.Bones[parent.ParentIndex];
                }
                else
                {
                    parent = null;
                }
            }
            while (parent != null);

            return boneParentMatrix;
        }

        float Multiplier = 2.25f;

        public void SetMuliplier(float multiplier)
        {
            Multiplier = multiplier;
        }

        private void SetBoneBoundingBox(FLVER2 f, FLVER.Bone b)
        {
            var multiplierVector = new System.Numerics.Vector3(Multiplier, Multiplier, Multiplier);
            var bb = GetBoundingBox(GetVerticesParentedToBone(f, b).Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToArray());
            if (bb.Maximum.LengthSquared() != 0 || bb.Minimum.LengthSquared() != 0)
            {
                var matrix = GetParentBoneMatrix(f, b);
                b.BoundingBoxMin = Vector3.Transform(bb.Minimum, Matrix.Invert(matrix)).ToNumerics();
                b.BoundingBoxMax = Vector3.Transform(bb.Maximum, Matrix.Invert(matrix)).ToNumerics();
                b.BoundingBoxMin = System.Numerics.Vector3.Multiply(b.BoundingBoxMin, multiplierVector);
                b.BoundingBoxMax = System.Numerics.Vector3.Multiply(b.BoundingBoxMax, multiplierVector);
            }
            else
            {
                b.BoundingBoxMin = new System.Numerics.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                b.BoundingBoxMax = new System.Numerics.Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }

        public void FixAllBoundingBoxes(FLVER2 f)
        {
            PrecalculatedBoneLists.Clear();

            foreach (var b in f.Bones)
            {
                var boneParentMatrix = Matrix.Scaling(b.Rotation.X);
                SetBoneBoundingBox(f, b);

            }

            var submeshBBs = new List<BoundingBox>();

            foreach (var sm in f.Meshes)
            {
                var bb = GetBoundingBox(sm.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToArray());
                if (bb.Maximum.LengthSquared() != 0 || bb.Minimum.LengthSquared() != 0)
                {
                    submeshBBs.Add(bb);
                    sm.BoundingBox = new FLVER2.Mesh.BoundingBoxes();
                    sm.BoundingBox.Min = bb.Minimum.ToNumerics();
                    sm.BoundingBox.Max = bb.Maximum.ToNumerics();
                }
                else
                {
                    sm.BoundingBox = null;
                }
            }

            if (submeshBBs.Count > 0)
            {
                var finalBB = submeshBBs[0];
                for (int i = 1; i < submeshBBs.Count; i++)
                {
                    finalBB = BoundingBox.Merge(finalBB, submeshBBs[i]);
                }

                var multiplierVector = new System.Numerics.Vector3(Multiplier, Multiplier, Multiplier);
                f.Header.BoundingBoxMin = new System.Numerics.Vector3(finalBB.Minimum.X, finalBB.Minimum.Y, finalBB.Minimum.Z);
                f.Header.BoundingBoxMax = new System.Numerics.Vector3(finalBB.Maximum.X, finalBB.Maximum.Y, finalBB.Maximum.Z);
                f.Header.BoundingBoxMin = System.Numerics.Vector3.Multiply(f.Header.BoundingBoxMin, multiplierVector);
                f.Header.BoundingBoxMax = System.Numerics.Vector3.Multiply(f.Header.BoundingBoxMax, multiplierVector);


            }
            else
            {
                f.Header.BoundingBoxMin = new System.Numerics.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                f.Header.BoundingBoxMax = new System.Numerics.Vector3(float.MinValue, float.MinValue, float.MinValue);

            }



        }
    }
}
