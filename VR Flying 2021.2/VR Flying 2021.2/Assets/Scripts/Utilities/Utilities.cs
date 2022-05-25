using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public static class Utilities
{
    public static float MetersPerMile = 1609.34f;
    
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
        
        for ( int y = 0; y < width; ++y )
        {
            for (int x = 0; x < height; ++x)
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

    public static float UnityDistanceToMiles(float dis)
    {
        return dis / MetersPerMile;
    }

    public static void MoveItem<T>(this List<T> list, int targetIndex, int oldIndex)
    {
        var item = list[oldIndex];
        list.Insert(targetIndex, item);

        if (targetIndex <= oldIndex)
        {
            ++oldIndex;
        }
        
        list.RemoveAt(oldIndex);
    }

    public static void MoveLastItemTo<T>(this List<T> list, int targetIndex)
    {
        if (targetIndex >= list.Count)
        {
            Debug.LogError(targetIndex + " is out of range [0, " + (list.Count - 1 + "]"));
            return;
        }
        list.Insert(targetIndex, list[list.Count - 1]);
        list.RemoveAt(list.Count - 1);
    }

    public static Vector2 Flatten(this Vector3 v3)
    {
        return new Vector2(v3.x, v3.z);
    }

    public static float BiLerp(float x, float y, float[,] map)
    {
        // round down to the nearest int
        float xmin = (int) x; // x1
        float ymin = (int) y; // y1
        float xmax = Mathf.CeilToInt(x); //x2
        float ymax = Mathf.CeilToInt(y); // y2
        
        float Q11 = map[(int)xmin, (int)ymin]; // bottom left point (x1, y1)
        float Q12 = map[(int)xmin, (int)ymax]; // top left point (x1, y2)
        float Q21 = map[(int)xmax, (int)ymin]; // bottom right point (x2, y1)
        float Q22 = map[(int)xmax, (int)ymax]; // top right point (x2, y2)

        // linear interpolation in the x direction
        // xupper + xlower = 1
        // R1: x on bottom left -> bottom right
        // R2: x on top left -> top right
        float xdis = xmax - xmin;
        float xupper = (xmax - x) / xdis; // the percentage of x -> xmax
        float xlower = (x - xmin) / xdis; // the percentage of xmin -> x
        float R1 = xupper * Q11 + xlower * Q21; // x on the line between Q11 and Q21, y = ymin
        float R2 = xupper * Q12 + xlower * Q22; // x on the line between Q12 and Q22, y = ymax

        // linear interpolation in the y direction (between R1 and R2)
        // yupper + ylower = 1
        float ydis = ymax - ymin;
        float yupper = (ymax - y) / ydis; // the percentage of y -> ymax
        float ylower = (y - ymin) / ydis; // the percentage of miny -> y
        float p = yupper * R1 + ylower * R2; // y on the line R1 -> R2

        return p;
    }
}
