using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiUILineRenderer : MaskableGraphic
{
    [Header("Line Settings")]
    public float thickness;
    public bool cull = false;

    public int trailCapacity = 50;
    public Vector2 anchorPosition;
    [SerializeField] private List<Vector2> trailPositions;
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (cull || trailPositions.Count == 0) return;

        for (int i = 0; i < trailPositions.Count - 1; ++i)
        {
            AddLine(trailPositions[i], trailPositions[i + 1], vh);
        }
        
        // AddLine(trailPositions[^trailPositions.Count], anchorPosition, vh);
    }

    public void AddPosition(Vector2 pos)
    {
        // add the position then check if the list is full
        trailPositions.Add(pos);
        if (trailPositions.Count > trailCapacity) trailPositions.RemoveAt(0);
    }
    
    private void AddLine(Vector2 p1, Vector2 p2, VertexHelper vh)
    {
        // init our temp variables with the input points
        Vector2 _point1 = p1;
        Vector2 _point2 = p2;

        // if point2 is above point1, we need to swap them
        // we need temp variables _point1 and _point2 so that this swap doesnt swap the values from the editor
        if (p2.y > p1.y)
        {
            _point1 = p2;
            _point2 = p1;
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
            float angle = Mathf.Atan2(topPointToBottomPoint.y, topPointToBottomPoint.x) * Mathf.Rad2Deg;
            
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

        int indexStart = vh.currentVertCount;
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

        vh.AddTriangle(indexStart, indexStart + 1, indexStart + 2);
        vh.AddTriangle(indexStart + 1, indexStart + 3, indexStart + 2);
    }

    protected override void Awake()
    {
        trailPositions = new List<Vector2>();
        ForceUpdate();
    }

    public void ForceUpdate()
    {
        SetAllDirty();
    }
    
}
