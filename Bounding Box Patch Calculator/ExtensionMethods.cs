using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounding_Box_Patch_Calculator
{
    public static class ExtensionMethods
    {
        public static System.Numerics.Vector3 ToNumerics(this SharpDX.Vector4 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static System.Numerics.Vector3 ToNumerics(this SharpDX.Vector3 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }
    }
}
