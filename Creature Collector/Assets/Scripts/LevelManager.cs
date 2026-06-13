using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelData[] levels;

    public EncounterChoiceUI encounterChoiceUI;
    public PlacementManager placementManager;
    public CaptureUI captureUI;

    public GameObject gameOverScreen;   // Optional: for when player 2 wins
    public GameObject allLevelsClearScreen; // Optional: end screen

    private int currentLevelIndex = 0;
    private EncounterOption chosenEncounter;
    private List<Creature> spawnedEnemies = new List<Creature>();
    private List<GameObject> spawnedEnemyPrefabs = new List<GameObject>();


    void Awake() => Instance = this;

    void Start()
    {
        LoadLevel(currentLevelIndex);
    }

    public void LoadLevel(int index)
    {
        currentLevelIndex = index;
        LevelData level = levels[index];

        // Build grid
        BlackBoard.gridManager.SetupGrid(level.gridWidth, level.gridHeight);

        // Center camera
        CameraController.Instance.CenterOnGrid(level.gridWidth, level.gridHeight);

        // Show encounter choice
        encounterChoiceUI.Show(level.encounterOptions, OnEncounterChosen);
    }
    List<Creature> GetAliveEnemies()
    {
        var list = new List<Creature>();

        foreach (var c in FindObjectsByType<Creature>(FindObjectsSortMode.None))
        {
            if (c.assignedPlayer == 1 && !c.dead)
                list.Add(c);
        }

        return list;
    }

    List<GameObject> BuildCaptureOptions()
    {
        Debug.Log($"BuildCaptureOptions: {spawnedEnemyPrefabs.Count} prefabs stored");
        return new List<GameObject>(spawnedEnemyPrefabs);
    }

    void OnEncounterChosen(EncounterOption chosen)
    {
        chosenEncounter = chosen;
        spawnedEnemies.Clear();

        LevelData level = levels[currentLevelIndex];

        // Auto-place enemy (Player 2) creatures on their side
        PlaceEnemyCreatures(chosen.enemyCreaturePrefabs, level);

        // Let Player 1 place their creatures
        GameObject[] playerPrefabs = PlayerRoster.Instance.GetCreaturesForLevel(level.playerCreatureCount);
        placementManager.StartPlacement(playerPrefabs, level.gridWidth, level.gridHeight, OnPlacementDone);
    }

    void PlaceEnemyCreatures(GameObject[] prefabs, LevelData level)
    {
        spawnedEnemyPrefabs.Clear();
        int x = level.gridWidth - 1;
        int[] ySlots = EvenlySpaced(prefabs.Length, level.gridHeight);

        for (int i = 0; i < prefabs.Length; i++)
        {
            Tile tile = BlackBoard.gridManager.map[x, ySlots[i]];
            Creature c = Instantiate(prefabs[i]).GetComponent<Creature>();
            c.assignedPlayer = 1;
            c.Initialize(tile);
            c.sourcePrefab = prefabs[i];
            spawnedEnemies.Add(c);
            spawnedEnemyPrefabs.Add(prefabs[i]); // store directly
        }
    }

    int[] EvenlySpaced(int count, int gridHeight)
    {
        int[] positions = new int[count];
        if (count == 1)
        {
            positions[0] = gridHeight / 2;
        }
        else
        {
            float step = (gridHeight - 1f) / (count - 1);
            for (int i = 0; i < count; i++)
                positions[i] = Mathf.RoundToInt(i * step);
        }
        return positions;
    }

    void OnPlacementDone()
    {
        BlackBoard.gameManager.StartBattle();
    }

    // Called by GameManager.CheckVictory() instead of showing its own screen
    public void OnBattleVictory(int winnerPlayer)
    {
        if (winnerPlayer == 0)
        {
            BlackBoard.gameManager.ClearSelectionState();
            BlackBoard.gameManager.SetCapturing(true);

            bool isLastLevel = currentLevelIndex >= levels.Length - 1;

            if (isLastLevel)
            {
                BlackBoard.gameManager.SetCapturing(false);
                if (allLevelsClearScreen != null)
                    allLevelsClearScreen.SetActive(true);
            }
            else
            {
                var options = BuildCaptureOptions();
                captureUI.Show(options, OnCaptureDone);
            }
        }
        else
        {
            if (gameOverScreen != null)
                gameOverScreen.SetActive(true);
        }
    }

    void OnCaptureDone(GameObject capturedPrefab)
    {
        BlackBoard.gameManager.SetCapturing(false);
        PlayerRoster.Instance.AddCreature(capturedPrefab);
        CleanupLevel();

        currentLevelIndex++;
        if (currentLevelIndex < levels.Length)
            LoadLevel(currentLevelIndex);
        else if (allLevelsClearScreen != null)
            allLevelsClearScreen.SetActive(true);
    }

    void CleanupLevel()
    {
        foreach (var c in FindObjectsByType<Creature>(FindObjectsSortMode.None))
            Destroy(c.gameObject);
        Tile.selectedTile = null;
        BlackBoard.gridManager.ResetGridHighlights();
    }
}