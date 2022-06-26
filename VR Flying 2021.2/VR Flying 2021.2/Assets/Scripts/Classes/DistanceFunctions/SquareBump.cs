using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Classes.DistanceFunctions
{
    public static class SquareBump
    {
        public static Dictionary<string, float> defaultOpts = new Dictionary<string, float>()
        {
            { "Exponent", 2f }
        };

        public static float[] defaultOptsArray = new[] { 2f };
        
        public static float Distance(int x, int y, int width, int height, float[] opts=null)
        {
            if (opts == null) opts = OptsToArray();
            
            // x and y [0, width], [0, height]
            // remap x and y to [-1, 1]
            // // map to [0, 1] -> [0, 2] -> [-1, 1]
            float nx = (((float)x / width) * 2) - 1;
            float ny = (((float)y / height) * 2) - 1;

            // 1 - (1-x^2) * (1 - y^2)
            return Mathf.Pow(1 - (1 - nx * nx) * (1 - ny * ny), 2);
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
