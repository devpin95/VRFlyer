using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct BuildAdvancedIndexJob : IJobParallelFor
{
    [ReadOnly] public uint meshSquares;
    [ReadOnly] public uint meshVerts;
    [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<uint> indices;
    
    public void Execute(int index)
    {
        uint square = (uint) index; // convert the index to a uint so we dont have to keep doing it
        
        uint row = square / meshSquares; // the row of the current square
        uint rowStartSquareIndex = row * meshSquares; // the index of the square at the beginning of this row
        uint distanceFromRowStart = square - rowStartSquareIndex; // how many squares are between the current square and the start of the row (this vertex number of vertices to the edge of the mesh)
        
        uint bottomLeft = rowStartSquareIndex + distanceFromRowStart;
        uint bottomRight = bottomLeft + 1;
        uint topLeft = bottomLeft + meshVerts;
        uint topRight = topLeft + 1;

        int startIndex = index * 6; // the current square * 6 for the number of triangles before this square
        indices[startIndex] = bottomLeft;
        indices[startIndex + 1] = topLeft;
        indices[startIndex + 2] = bottomRight;
        
        indices[startIndex + 3] = bottomRight;
        indices[startIndex + 4] = topLeft;
        indices[startIndex + 5] = topRight;
    }
}
