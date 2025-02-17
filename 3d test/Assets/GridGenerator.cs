using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab; // Prefab for the grid tiles

    public int gridWidth = 25;  // Number of tiles in the X direction
    public int gridHeight = 25; // Number of tiles in the Z direction
    public float tileSize = 4;  // Size of each tile

    void Start()
    {
        GenerateGrid(); // Call the function to generate the grid when the game starts
    }

    void GenerateGrid()
    {
        // Calculate offsets to center the grid at (0,0)
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;

        // Loop through each grid position and instantiate a tile
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Calculate the position of the tile
                Vector3 spawnPos = new Vector3(x * tileSize - offsetX, 0, z * tileSize - offsetZ);

                // Instantiate the tilePrefab at the calculated position
                Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
            }
        }
    }
}
