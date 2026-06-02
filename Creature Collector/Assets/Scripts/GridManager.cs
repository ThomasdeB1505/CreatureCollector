using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 10;
    public int height = 10;
    public GameObject tilePrefab;

    [Header("Materials")]
    public Material lightMaterial;
    public Material darkMaterial;
    public Material moveRangeMaterial;

    public Tile[,] map;

    public float tileHeightOffset = 0.05f;

    //for now it's both an array with a size of 3, and due to time constraints, I'm hard coding it this way
    public GameObject[] playerOneCreaturePrefabs;
    public GameObject[] playerTwoCreaturePrefabs;


    private void Awake()
    {
        BlackBoard.gridManager = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        map = new Tile[width, height];
        GenerateGrid();
        SpawnCreatures();
    }

    public void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isLight = (x + y) % 2 == 0;
                float heightOffset = isLight ? tileHeightOffset : 0f;
                Vector3 tilePosition = new Vector3(x - width / 2, heightOffset, y - height / 2);
                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
                tile.name = $"Tile {x},{y}";
                tile.transform.SetParent(transform);
                Renderer renderer = tile.GetComponentInChildren<Renderer>(); // make sure this is here
                renderer.material = new Material(isLight ? lightMaterial : darkMaterial);

                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.gridPosition = new Vector2Int(x, y);
                tileScript.originalColor = renderer.material.color;
                map[x,y] = tileScript;
            }
        }
    }

    public void SpawnCreatures()
    {
        Instantiate(playerOneCreaturePrefabs[0]).GetComponent<Creature>().Initialize(map[0,0]);
        Instantiate(playerOneCreaturePrefabs[1]).GetComponent<Creature>().Initialize(map[0,3]);
        Instantiate(playerOneCreaturePrefabs[2]).GetComponent<Creature>().Initialize(map[0,6]);

        Instantiate(playerTwoCreaturePrefabs[0]).GetComponent<Creature>().Initialize(map[6, 0]);
        Instantiate(playerTwoCreaturePrefabs[1]).GetComponent<Creature>().Initialize(map[6, 3]);
        Instantiate(playerTwoCreaturePrefabs[2]).GetComponent<Creature>().Initialize(map[6, 6]);

    }

    private List<Tile> GetTileNeighbors(Vector2Int tilePosition)
    {
        List<Tile> neighbors = new List<Tile>();

        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int posX = tilePosition.x + x;
                int posY = tilePosition.y + y;

                if (posX >= 0 && posY >= 0 && posX < width && posY < height && new Vector2Int(posX, posY) != tilePosition)
                {
                    neighbors.Add(map[posX, posY]);
                }
            }
        }

        return neighbors;
    }
    
    private bool IsDiagonal(Tile a, Tile b)
    {
        int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int dy = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
        return dx == 1 && dy == 1;
    }

    public void ResetGridHighlights()
    {
        foreach (Tile tile in map)
        {
            if (Tile.selectedTile != tile)
            {
                tile.inMoveRange = false;
                tile.SetMaterial(tile.originalMaterial);
            }
        }
    }
    public List<Tile> GetTilesInRange(Tile startTile, int range)
    {
        List<Tile> result = new List<Tile>();
        Queue<(Tile tile, int cost)> queue = new Queue<(Tile, int)>();
        Dictionary<Tile, int> visited = new Dictionary<Tile, int>();

        queue.Enqueue((startTile, 0));
        visited[startTile] = 0;

        while (queue.Count > 0)
        {
            var (current, cost) = queue.Dequeue();

            if (cost > 0) result.Add(current);

            foreach (Tile neighbor in GetTileNeighbors(current.gridPosition))
            {
                int moveCost = cost + (IsDiagonal(current, neighbor) ? 2 : 1);

                if (moveCost <= range && (!visited.ContainsKey(neighbor) || visited[neighbor] > moveCost))
                {
                    visited[neighbor] = moveCost;
                    queue.Enqueue((neighbor, moveCost));
                }
            }
        }
        return result;
    }

    public void HighlightMoveRange(Tile creatureTile, int range)
    {
        foreach (Tile tile in GetTilesInRange(creatureTile, range))
        {
            tile.inMoveRange = true;
            if (tile != Tile.selectedTile)
                tile.SetMaterial(moveRangeMaterial);
        }
    }
    public void HighlightAttackRange(Tile creatureTile, int moveRange, int attackRange)
    {
        List<Tile> moveTiles = GetTilesInRange(creatureTile, moveRange);
        List<Tile> attackTiles = GetTilesInRange(creatureTile, attackRange);

        foreach (Tile tile in attackTiles)
        {
            if (tile == Tile.selectedTile) continue;

            if (moveTiles.Contains(tile))
                tile.SetMaterial(tile.CombinedRangeMaterial);
            else
                tile.SetMaterial(tile.AttackRangeMaterial);
        }
    }
}
