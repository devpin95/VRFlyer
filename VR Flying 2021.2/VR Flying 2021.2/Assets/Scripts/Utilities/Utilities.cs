using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static float Remap (float from, float fromMin, float fromMax, float toMin,  float toMax) {
        // if ( from > fromMax || from < fromMin ) Debug.LogWarning("Value is outside initial range");
        
        var fromAbs  =  from - fromMin;
        var fromMaxAbs = fromMax - fromMin;      
       
        var normal = fromAbs / fromMaxAbs;
 
        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;
 
        var to = toAbs + toMin;
       
        return to;
    }
    
    public static T[] Flatten2DArray<T>(T[,] grid, int width, int height)
    {
        List<T> flatlist = new List<T>();
        
        for ( int x = 0; x < height; ++x )
        {
            for (int y = 0; y < width; ++y)
            {
                flatlist.Add(grid[x, y]);
            }
        }

        return flatlist.ToArray();
    }
    
    public static Sprite Tex2dToSprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    }

    public static string ArrayToString<T>(T[] array)
    {
        string res = "";

        for (int i = 0; i < array.Length; ++i)
        {
            if (i == array.Length - 1) res += array[i].ToString();
            else res += array[i].ToString() + ", ";
        }
        
        return res;
    }
}
