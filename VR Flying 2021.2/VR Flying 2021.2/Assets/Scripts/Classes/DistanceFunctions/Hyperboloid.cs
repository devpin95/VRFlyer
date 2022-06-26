using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Classes.DistanceFunctions
{
    public static class Hyperboloid
    {
        public static Dictionary<string, float> defaultOpts = new Dictionary<string, float>()
        {
            { "Constant", 0.1f },
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
            
            // sqrt(nx^I + ny^I + C^I) / (sqrt(1 + C^I) - C)
            // C float > 0
            // I int > 0
            float xsqr = Mathf.Pow(nx, opts[1]);
            float ysqr = Mathf.Pow(ny, opts[1]);
            float csqr = Mathf.Pow(opts[0], opts[1]);
            float normal = Mathf.Sqrt(1 + Mathf.Pow(opts[0], opts[1])) - opts[0];
            return Mathf.Sqrt(xsqr + ysqr + csqr) / normal;
        }
        
        public static float[] OptsToArray(Dictionary<string, float> targetOpts=null)
        {
            if (targetOpts == null) targetOpts = defaultOpts;
            
            float[] options = new float[2];
            options[0] = targetOpts["Constant"];
            options[1] = targetOpts["Exponent"];

            return options;
        }
    }
}
