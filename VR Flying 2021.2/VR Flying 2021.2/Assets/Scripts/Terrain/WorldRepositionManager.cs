using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRepositionManager : MonoBehaviour
{
    public float travelDistanceThreshold = 5000;
    public Transform playerTransform;
    
    public CEvent_Vector3 worldRepositionEvent;
    
    [Header("World Position")]
    public Vector3 worldPos = Vector3.zero;
    public Vector2 gridPos = Vector2.zero;
    public Vector4 gridQuad = Vector4.zero;
    public Vector4 currentGridQuad = Vector4.zero;

    [Header("Grid Objects")] 
    public List<MeshManager> terrainBlocks = new List<MeshManager>();
    
    [Header("Offsets")]
    [SerializeField] public Vector3 majorOffset = Vector3.zero;
    [SerializeField] public float distanceFromOrigin = 0;

    private float gridsize = 255 * 100;

    private Vector3 _playerOldPosition;

    // Start is called before the first frame update
    void Start()
    {
        worldPos = playerTransform.position;
        distanceFromOrigin += Vector3.Distance(playerTransform.position, Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        gridQuad = Vector4.zero;
        
        // the player's world position is their current position from the origin + how much we have reset back to the origin
        worldPos = playerTransform.position + majorOffset;
        
        // get the grid xz position, offset the worldPos by half the gridsize because each grid is positioned to it's center
        // flip the sign of the world pos so that the grid positions will be negative down and to the left of the origin
        // worldPos=(24245.4, 50627.77) gives gridPos=(0,1)
        // worldPos=(24747.4, 51222.15) gives gridPos=(0,0) WHY?????
        // float absx = ((worldPos.x - gridsize) / gridsize) * ( worldPos.x < 0 ? 1 : -1 );
        // float absz = ((worldPos.z - gridsize) / gridsize) * ( worldPos.z < 0 ? 1 : -1 );
        // gridPos = new Vector2(Mathf.Floor(absx + (worldPos.x > gridsize ? 1 : 0)), Mathf.Floor(absz + (worldPos.z > gridsize ? 1 : 0)));
        gridPos = new Vector2(Mathf.Floor(worldPos.x / gridsize), Mathf.Floor(worldPos.z / gridsize));

        Vector2 currentGridOrigin = new Vector2(gridPos.x * gridsize, gridPos.y * gridsize);

        if (worldPos.x > currentGridOrigin.x + (gridsize / 2))
        {
            // quad 1 or 4
            if (worldPos.z > currentGridOrigin.y + (gridsize / 2)) gridQuad.x = 1; // quad 1
            else gridQuad.w = 1; // quad 4
        }
        else
        {
            // quad 2 or 3
            if (worldPos.z > currentGridOrigin.y + (gridsize / 2)) gridQuad.y = 1; // quad 2
            else gridQuad.z = 1; // quad 3
        }
        
        // get the distance from the grid origin (bottom left corner) in the x and z direction
        // offset the worldPos by half the gridsize because each grid is positioned to it's center
        // take the abs of the position because we dont care about negative directions
        // mod by gridsize to get the distance in the range [0, gridsize]
        // float xquad = Mathf.Abs(worldPos.x) % gridsize;
        // float zquad = Mathf.Abs(worldPos.z) % gridsize;
        //
        // // get the quadrant the player is in of the current grid position
        // if (xquad > gridsize / 2)
        // {
        //     if (zquad > gridsize / 2) gridQuad.w = 1; // quad 2
        //     else gridQuad.z = 1; // quad 1
        // }
        // else // xquad <= gridsize / 2
        // {
        //     if (zquad > gridsize / 2) gridQuad.x = 1; // quad 3
        //     else gridQuad.y = 1; // quad 4
        // }


        bool distanceThresholdReached = Vector3.Distance(playerTransform.position, _playerOldPosition) > travelDistanceThreshold;
        if (currentGridQuad != gridQuad && distanceThresholdReached)
        {
            _playerOldPosition = playerTransform.position;
            MoveGridBlocks();
        }
    }

    public void MoveGridBlocks()
    {
        string list = "";
        List<Vector2> majorGridPositions = new List<Vector2>();
        majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y));
        list += "(" + (gridPos.x - 1) + "," + gridPos.y + ")";
            
        majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y));
        list += " (" + (gridPos.x + 1) + "," + gridPos.y + ")";
            
        majorGridPositions.Add(new Vector2(gridPos.x, gridPos.y - 1));
        list += " (" + gridPos.x + "," + (gridPos.y - 1) + ")";
            
        majorGridPositions.Add(new Vector2(gridPos.x, gridPos.y + 1));
        list += " (" + gridPos.x + "," + (gridPos.y + 1) + ")";

        // set a diagonal grid pos based on what quadrant the player is in over the currently active block
        if (gridQuad.x == 1) { // Quad 1
            majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y + 1)); // upper right
            majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y + 1)); // upper left
            majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y - 1)); // lower right
        }
        else if (gridQuad.y == 1) { // Quad 2
            majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y + 1)); // upper right
            majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y + 1)); // upper left
            majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y - 1)); // lower left
        }
        else if (gridQuad.z == 1) { // Quad 3
            majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y + 1)); // upper left
            majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y - 1)); // lower left
            majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y - 1)); // lower right
        }
        else if (gridQuad.w == 1) { // Quad 4
            majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y - 1)); // lower right
            majorGridPositions.Add(new Vector2(gridPos.x - 1, gridPos.y - 1)); // lower right
            majorGridPositions.Add(new Vector2(gridPos.x + 1, gridPos.y + 1)); // upper right
        }
        
        // Debug.Log("Active grid: " + gridPos + " New positions: " + list);

        List<MeshManager> availableBlocks = new List<MeshManager>(); // list of blocks that are not in positions that need to be filled
        
        // loop through each block and test if it's offset is one of the ones needed
        foreach (var block in terrainBlocks)
        {
            if (!majorGridPositions.Contains(block.gridOffset) && block.gridOffset != gridPos)
            {
                // Debug.Log("Making block at " + block.gridOffset + " available");
                // if the block is not in one of the needed offsets, add it to the list and flag is as available to move
                availableBlocks.Add(block);
            }
        }
            
        // loop through all of the positions and test if it is currently being filled by a block
        // if the pos does not currently have a block, we need to allocate one of the available meshes to it
        // if there is a block at the pos, we don't want to do anything
        foreach (var pos in majorGridPositions)
        {
            bool posNeedsFilled = true; // flag that the pos does not currently have a block
            
            // loop through each block and test if it is at the pos we are testing
            foreach (var block in terrainBlocks)
            {
                // if this pos is already taken, we don't want to give it to a new block
                if (pos == block.gridOffset)
                {
                    posNeedsFilled = false; 
                    break;
                }
            }

            // if we got through all of the blocks and didnt find a block with a matching offset, we need to move
            // one of the available blocks to this pos
            if (posNeedsFilled)
            {
                // Debug.Log("Filling pos " + pos);
                MeshManager targetBlock = availableBlocks[0];
                availableBlocks.Remove(targetBlock);
                targetBlock.SetGridPosition(pos);
            }
        }
        
        // lower corner
        // MeshManager lowerCornerBlock = availableBlocks[0];
        // availableBlocks.Remove(lowerCornerBlock);
        // Vector2 lowerCornerTargetPos = Vector2.zero;
        //
        // // upper corner
        // MeshManager upperCornerBlock = availableBlocks[0];
        // availableBlocks.Remove(upperCornerBlock);
        // Vector2 upperCornerTargetPos = Vector2.zero;
        //
        // if ((int) gridQuad.x == 1 || (int) gridQuad.y == 1)
        // {
        //     lowerCornerTargetPos = new Vector2(gridPos.x + 1, gridPos.y - 1);
        //     upperCornerTargetPos = new Vector2(gridPos.x + 1, gridPos.y + 1);
        // }
        //
        // if ((int) gridQuad.z == 1 || (int) gridQuad.w == 1)
        // {
        //     lowerCornerTargetPos = new Vector2(gridPos.x - 1, gridPos.y + 1);
        //     upperCornerTargetPos = new Vector2(gridPos.x - 1, gridPos.y - 1);
        // }
        //
        // lowerCornerBlock.SetGridPosition(lowerCornerTargetPos);
        // upperCornerBlock.SetGridPosition(upperCornerTargetPos);
            
        // Debug.Log("Reset grid. Active pos: " + gridPos + " major positions: " + list);
        currentGridQuad = gridQuad;
    }

    public void Reposition()
    {
        Debug.Log("Resetting position");
        float dis = Vector3.Distance(playerTransform.position, Vector3.zero);
        distanceFromOrigin += dis;
        majorOffset += playerTransform.position;
        Vector3 offset = -playerTransform.position;
        worldRepositionEvent.Raise(offset);
    }
}
