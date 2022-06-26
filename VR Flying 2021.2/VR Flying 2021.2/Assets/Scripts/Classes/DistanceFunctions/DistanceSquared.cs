using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Classes.DistanceFunctions
{
    public static class DistanceSquared
    {
        public static Dictionary<string, float> defaultOpts = new Dictionary<string, float>()
        {
            { "Exponent", 2f }
        };
        
        
        public static float Distance(int x, int y, int width, int height, float[] opts=null)
        {
            if (opts == null) opts = OptsToArray();
            
            // x and y [0, width], [0, height]
            // remap x and y to [-1, 1]
            // // map to [0, 1] -> [0, 2] -> [-1, 1]
            int nx = ((x / width) * 2) - 1;
            int ny = ((y / height) * 2) - 1;
            
            // x^r + y^r
            // r int > 0
            float xsqr = Mathf.Pow(nx, (int)opts[0]);
            float ysqr = Mathf.Pow(ny, (int)opts[0]);
            return xsqr + ysqr;
        }
        
        public static float[] OptsToArray(Dictionary<string, float> targetOpts=null)
        {
            if (targetOpts == null) targetOpts = defaultOpts;
            
            float[] options = new float[1];
            options[0] = targetOpts["Exponent"];

            return options;
        }
    }
}
