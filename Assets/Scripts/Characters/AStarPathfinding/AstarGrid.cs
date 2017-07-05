using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstarGrid : MonoBehaviour {

    public Transform[] pawns;
    public LayerMask GroundLayer;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    AstarNode[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;


    private void Start()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    void CreateGrid ()
    {
        grid = new AstarNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, GroundLayer));
                grid[x, y] = new AstarNode(walkable, worldPoint, x, y);
            }
        }
    }

    public List<AstarNode> GetNeighbors(AstarNode node)
    {
        List<AstarNode> neighbors = new List<AstarNode>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    public AstarNode NodeFromWorldPoint (Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    public List<AstarNode> path;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));
        if (grid != null)
        {
            List<AstarNode> pawnNodes = new List<AstarNode>(); 

            foreach (Transform pawn in pawns)
            {
                pawnNodes.Add (NodeFromWorldPoint(pawn.position));
                Gizmos.DrawSphere(pawn.position, nodeRadius);
                Debug.Log("Node for Pawn = " + NodeFromWorldPoint(pawn.position).worldPosition);
            }



            foreach (AstarNode node in grid)
            {
                Gizmos.color = (node.walkable) ? Color.white : Color.red;

                foreach (AstarNode pawnNode in pawnNodes)
                {
                    if (pawnNode.worldPosition == node.worldPosition)
                        Gizmos.color = Color.green;
                }

                if (path != null)
                {
                    if (path.Contains(node))
                        Gizmos.color = Color.black;
            }

                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }
}
