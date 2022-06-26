using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Classes.DistanceFunctions
{
    public class TrigProduct
    {
        private static float halfpi = Mathf.PI / 2;
        
        public static Dictionary<string, float> defaultOpts = new Dictionary<string, float>()
        {
            { "Exponent1", 2f },
            { "Exponent2", 2f },
            { "Exponent3", 1f }
        };
        
        
        public static float Distance(int x, int y, int width, int height, float[] opts=null)
        {
            if (opts == null) opts = OptsToArray();
            
            // x and y [0, width], [0, height]
            // remap x and y to [-1, 1]
            // // map to [0, 1] -> [0, 2] -> [-1, 1]
            int nx = ((x / width) * 2) - 1;
            int ny = ((y / height) * 2) - 1;

            // 1 - [ cos(x^e1) * cos(y^e2) ]^e3
            // e1, e2, e2 > 0
            float a1 = Mathf.Pow(nx, opts[0]) * halfpi;
            float a2 = Mathf.Pow(ny, opts[1]) * halfpi;
            float cos1 = Mathf.Cos(a1);
            float cos2 = Mathf.Cos(a2);
            float prod = cos1 * cos2;
            return 1 - Mathf.Pow(prod, opts[2]);
        }
        
        public static float[] OptsToArray(Dictionary<string, float> targetOpts=null)
        {
            if (targetOpts == null) targetOpts = defaultOpts;
            
            float[] options = new float[2];
            options[0] = targetOpts["Exponent1"];
            options[1] = targetOpts["Exponent2"];
            options[2] = targetOpts["Exponent3"];

            return options;
        }
    }   
}
