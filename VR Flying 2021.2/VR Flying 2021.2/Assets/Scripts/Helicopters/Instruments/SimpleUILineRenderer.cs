using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class SimpleUILineRenderer : MaskableGraphic
{
    [Header("Line Settings")]
    public float thickness;
    public float lineMinLimit = 0.02f;
    [FormerlySerializedAs("culled")] public bool cull = false;
    [SerializeField] private float distance;

    public Vector2 point1;
    public Vector2 point2;

    [Header("Debug")] 
    public bool debug = false;
    public float angle;
    public float vertexBoxThickness = 0.005f;
    private bool start = true;
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (cull) return;

        // init our temp variables with the input points
        Vector2 _point1 = point1;
        Vector2 _point2 = point2;

        // if point2 is above point1, we need to swap them
        // we need temp variables _point1 and _point2 so that this swap doesnt swap the values from the editor
        if (point2.y > point1.y)
        {
            _point1 = point2;
            _point2 = point1;
        }

        Vector2 left = Vector2.left * thickness;
        Vector2 right = Vector2.right * thickness;

        // if the line is either horizontal or vertical, we dont need to do any math to angle the verts
        bool needToAngleVerts = true;
        if (_point1.y == _point2.y) // horizontal line
        {
            left = Vector2.up * thickness;
            right = Vector2.down * thickness;
            needToAngleVerts = false;
        } 
        else if (_point1.x == _point2.x) needToAngleVerts = false; // vertical line

        // the line isnt a special case so we need to angle the vertices around it's center point
        if (needToAngleVerts)
        {
            // get the vector pointing from the top point to the bottom point
            Vector2 topPointToBottomPoint = _point2 - _point1;
            
            // get the angle in degrees between the line and the x-axis (it will be in [0, -180])
            angle = Mathf.Atan2(topPointToBottomPoint.y, topPointToBottomPoint.x) * Mathf.Rad2Deg;
            
            if (Mathf.Abs(angle) < 90)
            {
                // if the angle is in quadrant 4 then we need to add 90 degrees to the angle to get the top left
                // vert to drop into quadrant 3 (we want it to be a positive angle so that it rotates counter-clockwise)
                left = left.Rotate(Mathf.Abs(angle + 90));
                right = right.Rotate(Mathf.Abs(angle + 90));
            }
            else if (Mathf.Abs((angle)) >= 90)
            {
                // if the angle is in quadrant 3 then we need to find how far past -90 degrees the angle is (how far
                // into quadrant 3 the angle is from quad 4). We want the correctAngle to be negative so that the vert
                // rotates clockwise around the line point
                float correctAngle = 90 - Mathf.Abs(angle);
                left = left.Rotate(correctAngle);
                right = right.Rotate(correctAngle);
            }
        }


        // left = left.Rotate(angle);
        // right = right.Rotate(angle - 90);

        UIVertex vertex = UIVertex.simpleVert;
        
        // 0
        // top left
        vertex.color = color;
        vertex.position = _point1 + left;
        vh.AddVert(vertex);
        
        // 1
        // top right
        vertex.color = color;
        vertex.position = _point1 + right;
        vh.AddVert(vertex);
        
        // 2
        // bottom left
        vertex.color = color;
        vertex.position = _point2 + left;
        vh.AddVert(vertex);
        
        // 3
        // bottom right
        vertex.color = color;
        vertex.position = _point2 + right;
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(1, 3, 2);

        if (debug)
        {
            ShowDebugMesh(vh, vertex);
        }
    }

    protected override void Awake()
    {
        SetAllDirty();
    }

    public void ForceUpdate()
    {
        SetAllDirty();
    }

    private void ShowDebugMesh(VertexHelper vh, UIVertex vertex)
    {
        vertex.color = Color.green;
        vertex.position = new Vector3(point1.x + vertexBoxThickness, point1.y + vertexBoxThickness, 0);
        vh.AddVert(vertex); // 4
        vertex.color = Color.green;
        vertex.position = new Vector3(point1.x + vertexBoxThickness, point1.y - vertexBoxThickness, 0);
        vh.AddVert(vertex); // 5
        vertex.color = Color.green;
        vertex.position = new Vector3(point1.x - vertexBoxThickness, point1.y + vertexBoxThickness, 0);
        vh.AddVert(vertex); // 6
        vertex.color = Color.green;
        vertex.position = new Vector3(point1.x - vertexBoxThickness, point1.y - vertexBoxThickness, 0);
        vh.AddVert(vertex); // 7
        
        vh.AddTriangle(4, 5, 6);
        vh.AddTriangle(7, 6, 5);
        
        vertex.color = Color.green;
        vertex.position = new Vector3(point2.x + vertexBoxThickness, point2.y + vertexBoxThickness, 0);
        vh.AddVert(vertex); // 8
        vertex.color = Color.green;
        vertex.position = new Vector3(point2.x + vertexBoxThickness, point2.y - vertexBoxThickness, 0);
        vh.AddVert(vertex); // 9
        vertex.color = Color.green;
        vertex.position = new Vector3(point2.x - vertexBoxThickness, point2.y + vertexBoxThickness, 0);
        vh.AddVert(vertex); // 10
        vertex.color = Color.green;
        vertex.position = new Vector3(point2.x - vertexBoxThickness, point2.y - vertexBoxThickness, 0);
        vh.AddVert(vertex); // 11
        
        vh.AddTriangle(8, 9, 10);
        vh.AddTriangle(11, 10, 9);
    }
}

public static class Vector2Extension {
     
    public static Vector2 Rotate(this Vector2 v, float degrees) {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
         
        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }
}
