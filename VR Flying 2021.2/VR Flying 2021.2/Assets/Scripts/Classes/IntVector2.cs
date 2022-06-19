using System;
using UnityEngine;
using UnityEngine.Assertions;

public class IntVector2
{
    public int x = 0;
    public int y = 0;
    
    public IntVector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public IntVector2(IntVector2 prev)
    {
        this.x = prev.x;
        this.y = prev.y;
    }

    public static bool operator ==(IntVector2 a, IntVector2 b)
    {
        return a?.x == b?.x && a?.y == b?.y;
    }
    
    public static bool operator !=(IntVector2 a, IntVector2 b)
    {
        return a?.x != b?.x || a?.y != b?.y;
    }

    public static IntVector2 operator +(IntVector2 a, IntVector2 b)
    {
        return new IntVector2(a.x + b.x, a.y + b.y);
    }

    public override string ToString()
    {
        return "IntVector2(" + x + ", " + y + ")";
    }

    // static values
    private static readonly IntVector2 zeroVector = new IntVector2(0, 0);
    private static readonly IntVector2 oneVector = new IntVector2(1, 1);
    private static readonly IntVector2 rightVector = new IntVector2(1, 0);
    private static readonly IntVector2 leftVector = new IntVector2(-1, 0);
    private static readonly IntVector2 upVector = new IntVector2(0, 1);
    private static readonly IntVector2 downVector = new IntVector2(0, -1);

    // static value gEtTErs
    public static IntVector2 zero => zeroVector;
    public static IntVector2 one => oneVector;
    public static IntVector2 right => rightVector;
    public static IntVector2 left => leftVector;
    public static IntVector2 up => upVector;
    public static IntVector2 down => downVector;
}
