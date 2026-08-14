using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelData[] levels;

    public EncounterChoiceUI encounterChoiceUI;
    public PlacementManager placementManager;
    public CraftingManager craftingManager;

    public GameObject gameOverScreen;
    public GameObject allLevelsClearScreen;
    public GameObject combatCanvas; // assign in Inspector: the canvas holding ActionUI, turn text, action points, etc.

    private int currentLevelIndex = 0;
    private EncounterOption chosenEncounter;
    private HashSet<EssenceType> essenceEarnedThisEncounter = new();

    void Awake() => Instance = this;

    void Start()
    {
        // Forced initial form + craft before anything else
        craftingManager.ForceInitialCraft(FormType.Attack, () => LoadLevel(currentLevelIndex));
    }

    public void LoadLevel(int index)
    {
        currentLevelIndex = index;
        LevelData level = levels[index];

        combatCanvas.SetActive(false); // hub/prep phase - hide combat UI

        // Grid size is now fixed on GridManager itself (inspector), not per-level.
        BlackBoard.gridManager.SetupGrid();
        CameraController.Instance.CenterOnGrid(BlackBoard.gridManager.width, BlackBoard.gridManager.height);

        encounterChoiceUI.Show(level.encounterOptions, OnEncounterChosen);
    }

    void OnEncounterChosen(EncounterOption chosen)
    {
        chosenEncounter = chosen;
        essenceEarnedThisEncounter.Clear();

        LevelData level = levels[currentLevelIndex];

        PlaceEnemyCreatures(chosen.enemyCreaturePrefabs, level);

        // Obstacles go in after enemies are placed and before the player places,
        // so they can avoid both reserved columns.
        BlackBoard.gridManager.PlaceObstacles();

        combatCanvas.SetActive(true); // turn back on before placement/combat begins

        GameObject[] playerPrefabs = PlayerRoster.Instance.GetCreaturesForLevel(level.playerCreatureCount);
        placementManager.StartPlacement(playerPrefabs, BlackBoard.gridManager.width, BlackBoard.gridManager.height, OnPlacementDone);
    }

    void PlaceEnemyCreatures(GameObject[] prefabs, LevelData level)
    {
        int x = BlackBoard.gridManager.width - 1;
        int[] ySlots = EvenlySpaced(prefabs.Length, BlackBoard.gridManager.height);

        for (int i = 0; i < prefabs.Length; i++)
        {
            Tile tile = BlackBoard.gridManager.map[x, ySlots[i]];
            Creature c = Instantiate(prefabs[i]).GetComponent<Creature>();
            c.assignedPlayer = 1;
            c.Initialize(tile);
            c.sourcePrefab = prefabs[i];
        }
    }

    int[] EvenlySpaced(int count, int gridHeight)
    {
        int[] positions = new int[count];
        if (count == 1) { positions[0] = gridHeight / 2; return positions; }
        float step = (gridHeight - 1f) / (count - 1);
        for (int i = 0; i < count; i++) positions[i] = Mathf.RoundToInt(i * step);
        return positions;
    }

    void OnPlacementDone()
    {
        BlackBoard.gameManager.StartBattle();
    }

    // Called by Creature.Die() whenever an enemy (assignedPlayer == 1) dies
    public void OnEnemyCreatureDied(Creature enemy)
    {
        craftingManager.AddEssence(enemy.essenceDropType);
        essenceEarnedThisEncounter.Add(enemy.essenceDropType);
    }

    public void OnBattleVictory(int winnerPlayer)
    {
        if (winnerPlayer == 0)
        {
            BlackBoard.gameManager.ClearSelectionState();

            bool isLastLevel = currentLevelIndex >= levels.Length - 1;
            craftingManager.AddForm(chosenEncounter.formReward);

            if (isLastLevel)
            {
                if (allLevelsClearScreen != null)
                    allLevelsClearScreen.SetActive(true);
            }
            else
            {
                CleanupLevel();
                combatCanvas.SetActive(false);
                craftingManager.ShowHub(OnHubDone);
            }
        }
        else
        {
            if (gameOverScreen != null)
                gameOverScreen.SetActive(true);
        }
    }

    void OnHubDone()
    {
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