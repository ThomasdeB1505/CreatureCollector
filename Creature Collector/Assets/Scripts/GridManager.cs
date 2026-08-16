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
    public Material aoeRadiusMaterial; // assign in Inspector, e.g. orange or purple

    [Header("Obstacles")]
    public GameObject obstaclePrefab;
    public int obstacleCount = 6;

    public Transform arenaTransform;
    public Vector3 arenaBaseScale = Vector3.one;

    public Tile[,] map;

    public float tileHeightOffset = 0.05f;

    public GameObject[] playerOneCreaturePrefabs;
    public GameObject[] playerTwoCreaturePrefabs;

    private void Awake()
    {
        BlackBoard.gridManager = this;
    }

    void Start() { }

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
                Renderer renderer = tile.GetComponentInChildren<Renderer>();
                renderer.material = new Material(isLight ? lightMaterial : darkMaterial);

                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.gridPosition = new Vector2Int(x, y);
                tileScript.originalColor = renderer.material.color;
                map[x, y] = tileScript;
            }
        }
    }

    public void SpawnCreatures()
    {
        int midY = height / 2;
        int quarterY = height / 4;

        Instantiate(playerOneCreaturePrefabs[0]).GetComponent<Creature>().Initialize(map[0, quarterY]);
        Instantiate(playerOneCreaturePrefabs[1]).GetComponent<Creature>().Initialize(map[0, midY]);
        Instantiate(playerOneCreaturePrefabs[2]).GetComponent<Creature>().Initialize(map[0, height - 1 - quarterY]);

        Instantiate(playerTwoCreaturePrefabs[0]).GetComponent<Creature>().Initialize(map[width - 1, quarterY]);
        Instantiate(playerTwoCreaturePrefabs[1]).GetComponent<Creature>().Initialize(map[width - 1, midY]);
        Instantiate(playerTwoCreaturePrefabs[2]).GetComponent<Creature>().Initialize(map[width - 1, height - 1 - quarterY]);
    }

    private List<Tile> GetTileNeighbors(Vector2Int tilePosition)
    {
        List<Tile> neighbors = new List<Tile>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int posX = tilePosition.x + x;
                int posY = tilePosition.y + y;

                if (posX >= 0 && posY >= 0 && posX < width && posY < height && new Vector2Int(posX, posY) != tilePosition)
                {
                    Tile neighborTile = map[posX, posY];
                    // Blocked tiles (e.g. Defensive Stance's second tile, or an obstacle) can't be passed through or landed on
                    if (!neighborTile.blocked)
                        neighbors.Add(neighborTile);
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

    public Tile GetTileAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= width || pos.y >= height) return null;
        return map[pos.x, pos.y];
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

    // minRange/maxRange version. minRange = 1 reproduces the old behavior (excludes the creature's own tile)
    public List<Tile> GetTilesInRange(Tile startTile, int minRange, int maxRange)
    {
        List<Tile> result = new List<Tile>();
        Queue<(Tile tile, int cost)> queue = new Queue<(Tile, int)>();
        Dictionary<Tile, int> visited = new Dictionary<Tile, int>();

        queue.Enqueue((startTile, 0));
        visited[startTile] = 0;

        while (queue.Count > 0)
        {
            var (current, cost) = queue.Dequeue();

            if (cost > 0 && cost >= minRange)
                result.Add(current);

            foreach (Tile neighbor in GetTileNeighbors(current.gridPosition))
            {
                int moveCost = cost + (IsDiagonal(current, neighbor) ? 2 : 1);

                if (moveCost <= maxRange && (!visited.ContainsKey(neighbor) || visited[neighbor] > moveCost))
                {
                    visited[neighbor] = moveCost;
                    queue.Enqueue((neighbor, moveCost));
                }
            }
        }
        return result;
    }

    // Backwards-compatible overload (minRange = 1, same as the old behavior)
    public List<Tile> GetTilesInRange(Tile startTile, int maxRange)
    {
        return GetTilesInRange(startTile, 1, maxRange);
    }

    public void HighlightMoveRange(Tile creatureTile, int minRange, int maxRange)
    {
        foreach (Tile tile in GetTilesInRange(creatureTile, minRange, maxRange))
        {
            tile.inMoveRange = true;
            if (tile != Tile.selectedTile)
                tile.SetMaterial(moveRangeMaterial);
        }
    }

    public void HighlightAttackRange(Tile creatureTile, int moveMinRange, int moveMaxRange, int attackMinRange, int attackMaxRange)
    {
        List<Tile> moveTiles = GetTilesInRange(creatureTile, moveMinRange, moveMaxRange);
        List<Tile> attackTiles = GetTilesInRange(creatureTile, attackMinRange, attackMaxRange);

        foreach (Tile tile in attackTiles)
        {
            if (tile == Tile.selectedTile) continue;

            if (moveTiles.Contains(tile))
                tile.SetMaterial(tile.CombinedRangeMaterial);
            else
                tile.SetMaterial(tile.AttackRangeMaterial);
        }
    }

    // Rebuilds the grid at its current inspector-set width/height.
    // Use this instead of SetupGrid(w, h) now that grid size is fixed, not per-level.
    public void SetupGrid()
    {
        SetupGrid(width, height);
    }

    public void SetupGrid(int newWidth, int newHeight)
    {
        if (map != null)
        {
            foreach (Tile t in map)
                if (t != null) Destroy(t.gameObject);
        }

        width = newWidth;
        height = newHeight;
        map = new Tile[width, height];
        GenerateGrid();
        ScaleArena();
    }

    // Tiles that must never get an obstacle: the player's placement column (x == 0)
    // and the enemy spawn column (x == width - 1).
    private List<Vector2Int> GetReservedPositions()
    {
        List<Vector2Int> reserved = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            reserved.Add(new Vector2Int(0, y));
            reserved.Add(new Vector2Int(width - 1, y));
        }
        return reserved;
    }

    // Randomly places obstacleCount obstacles on the grid, skipping reserved columns.
    // Call this AFTER enemy creatures are placed and BEFORE player placement starts -
    // NOT inside SetupGrid(), since it depends on things that happen after grid generation.
    public void PlaceObstacles()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("GridManager.PlaceObstacles: obstaclePrefab is not assigned in the inspector.");
            return;
        }

        List<Vector2Int> reserved = GetReservedPositions();
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!reserved.Contains(pos))
                    candidates.Add(pos);
            }

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int count = Mathf.Min(obstacleCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            Tile tile = GetTileAt(candidates[i]);
            GameObject obstacleObj = Instantiate(obstaclePrefab, transform);
            obstacleObj.GetComponent<Obstacle>().Initialize(tile);
            tile.blocked = true;
        }
    }

    void ScaleArena()
    {
        if (arenaTransform == null) return;

        float xRatio = (float)width / 7f;
        float zRatio = (float)height / 7f;

        arenaTransform.localScale = new Vector3(
            arenaBaseScale.x * xRatio,
            arenaBaseScale.y,
            arenaBaseScale.z * zRatio
        );

        arenaTransform.position = new Vector3(0, arenaTransform.position.y, 0);
    }
}