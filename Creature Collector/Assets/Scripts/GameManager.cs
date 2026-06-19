using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    //handles the turns and input
    public int amountOfPlayers;
    int currentPlayer;

    public int actionsPerTurn;
    int currentTurnActions;

    Creature selectedCreature;

    public TextMeshProUGUI turnText;

    public ActionPointsUI actionPointsUI;

    public Material[] playerSkyboxes;
    public GameObject victoryScreen;
    public TextMeshProUGUI victoryText;

    public int deathPoints = 0;
    public TextMeshProUGUI deathPointsText;
    public Button evolveButton;
    private bool battleStarted = false;
    private bool isCapturing = false;
    public void SetCapturing(bool value) => isCapturing = value;
    public Material[] playerMaterials;

    // ── Evolution choice UI ─────────────────────────────────────────────────
    [Header("Evolution Choice")]
    [Tooltip("The EvolutionChoiceUI panel in the scene.")]
    public EvolutionChoiceUI evolutionChoiceUI;



    private void Awake()
    {
        BlackBoard.gameManager = this;
    }

    public Creature GetSelectedCreature()
    {
        return selectedCreature;
    }

    private void Start()
    {
        StartTurn();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
            Deselect();
    }

    public void StartBattle()
    {
        battleStarted = true;
        deathPoints = 0;
        UpdateDeathPointsUI();
        currentPlayer = 0;
        StartTurn();
    }

    void StartTurn()
    {
        currentTurnActions = actionsPerTurn;
        selectedCreature = null;
        actionPointsUI.SetupCircles(actionsPerTurn);
        actionPointsUI.UpdateCircles(currentTurnActions, actionsPerTurn);
        if (playerSkyboxes != null && playerSkyboxes.Length > currentPlayer)
            RenderSettings.skybox = playerSkyboxes[currentPlayer];
        UpdateTurnUI();
    }

    public void EndTurn()
    {
        currentPlayer++;
        if (currentPlayer >= amountOfPlayers)
            currentPlayer = 0;
        StartTurn();

        if (currentPlayer == 1)
            StartCoroutine(SimpleAI.Instance.TakeTurn(actionsPerTurn));
    }

    void UpdateTurnUI()
    {
        turnText.text = currentPlayer == 0 ? "Your Turn" : "Opponent's Turn";
    }

    void SpendAction(int actionCost)
    {
        currentTurnActions -= actionCost;
        actionPointsUI.UpdateCircles(currentTurnActions, actionsPerTurn);
    }

    bool HasActionsLeft()
    {
        return currentTurnActions > 0;
    }

    public void ClickOnTile(Tile _clicked)
    {
        if (isCapturing) return;
        if (PlacementManager.Instance.IsPlacing)
        {
            PlacementManager.Instance.HandleTileClick(_clicked);
            return;
        }
        if (selectedCreature != null)
        {
            if (_clicked == selectedCreature.currentTile)
            {
                Deselect();
                return;
            }

            if (_clicked.currentCreatureOnTile != null)
            {
                if (_clicked.currentCreatureOnTile.dead)
                {
                    Deselect();
                    return;
                }

                if (_clicked.currentCreatureOnTile.assignedPlayer == currentPlayer)
                {
                    selectedCreature = _clicked.currentCreatureOnTile;
                    RefreshHighlights();
                    return;
                }

                if (!HasActionsLeft()) { Deselect(); return; }

                List<Tile> attackRange = BlackBoard.gridManager.GetTilesInRange(
                    selectedCreature.currentTile, selectedCreature.attackRange);

                if (!attackRange.Contains(_clicked))
                {
                    Deselect();
                    return;
                }

                selectedCreature.Attack(_clicked.currentCreatureOnTile);
                SpendAction(selectedCreature.attackActionCost);
                Deselect();
            }
            else
            {
                if (!HasActionsLeft()) { Deselect(); return; }

                List<Tile> moveRange = BlackBoard.gridManager.GetTilesInRange(
                    selectedCreature.currentTile, selectedCreature.moveRange);

                if (!moveRange.Contains(_clicked))
                {
                    Deselect();
                    return;
                }

                selectedCreature.Moveto(_clicked.transform.position, _clicked);
                SpendAction(selectedCreature.moveActionCost);
                Deselect();
            }
        }
        else
        {
            if (_clicked.currentCreatureOnTile != null
                && !_clicked.currentCreatureOnTile.dead
                && _clicked.currentCreatureOnTile.assignedPlayer == currentPlayer)
            {
                selectedCreature = _clicked.currentCreatureOnTile;
                RefreshHighlights();
                return;
            }
            Deselect();
        }
    }

    public void RefreshHighlights()
    {
        BlackBoard.gridManager.ResetGridHighlights();
        if (selectedCreature != null)
        {
            BlackBoard.gridManager.HighlightMoveRange(selectedCreature.currentTile, selectedCreature.moveRange);
            BlackBoard.gridManager.HighlightAttackRange(selectedCreature.currentTile, selectedCreature.moveRange, selectedCreature.attackRange);
        }
        UpdateDeathPointsUI();
    }

    void Deselect()
    {
        selectedCreature = null;
        BlackBoard.gridManager.ResetGridHighlights();
        if (Tile.selectedTile != null)
        {
            Tile.selectedTile.SetMaterial(Tile.selectedTile.originalMaterial);
            Tile.selectedTile = null;
        }
        UpdateDeathPointsUI();
    }

    public void OnPlayerCreatureDied()
    {
        deathPoints++;
        UpdateDeathPointsUI();
    }

    void UpdateDeathPointsUI()
    {
        if (deathPointsText != null)
            deathPointsText.text = "Evolutions available: " + deathPoints;

        // Button is interactable when the selected creature has at least one
        // evolution option available (either slot A or slot B is non-null).
        if (evolveButton != null)
        {
            bool canEvolve = deathPoints > 0
                && selectedCreature != null
                && !selectedCreature.isEvolved
                && (selectedCreature.evolvedFormPrefab != null
                    || selectedCreature.evolvedFormPrefabB != null);

            evolveButton.interactable = canEvolve;
        }
    }

    // ── Evolution (two-choice flow) ──────────────────────────────────────────

    /// <summary>
    /// Called when the player presses the Evolve button.
    /// Opens the choice panel if both options exist; skips straight to
    /// ExecuteEvolution if only one option is configured.
    /// </summary>
    public void TryEvolveSelected()
    {
        if (selectedCreature == null) return;
        if (selectedCreature.isEvolved) return;
        if (deathPoints <= 0) return;
        if (!HasActionsLeft()) return;

        bool hasA = selectedCreature.evolvedFormPrefab != null;
        bool hasB = selectedCreature.evolvedFormPrefabB != null;

        if (!hasA && !hasB) return;    // nothing to evolve into

        // If only one option is configured, skip the choice panel entirely.
        if (hasA && !hasB) { ExecuteEvolution(selectedCreature.evolvedFormPrefab); return; }
        if (!hasA && hasB) { ExecuteEvolution(selectedCreature.evolvedFormPrefabB); return; }

        // Both options available — show the choice UI.
        if (evolutionChoiceUI == null)
        {
            Debug.LogWarning("GameManager: no EvolutionChoiceUI assigned — falling back to option A.");
            ExecuteEvolution(selectedCreature.evolvedFormPrefab);
            return;
        }

        evolutionChoiceUI.Show(
            selectedCreature.evolvedFormPrefab,
            selectedCreature.evolutionSpriteA,
            selectedCreature.evolutionLabelA,

            selectedCreature.evolvedFormPrefabB,
            selectedCreature.evolutionSpriteB,
            selectedCreature.evolutionLabelB
        );
    }

    /// <summary>
    /// Replaces selectedCreature with the chosen evolved prefab.
    /// Called either directly (single option) or by EvolutionChoiceUI callback.
    /// </summary>
    public void ExecuteEvolution(GameObject chosenPrefab)
    {
        if (chosenPrefab == null) return;
        if (selectedCreature == null) return;

        Tile tile = selectedCreature.currentTile;
        int player = selectedCreature.assignedPlayer;

        GameObject oldObj = selectedCreature.gameObject;

        Creature evolved = Instantiate(chosenPrefab).GetComponent<Creature>();
        evolved.assignedPlayer = player;
        evolved.isEvolved = true;

        tile.currentCreatureOnTile = null;   // break old reference before Destroy
        Destroy(oldObj);

        evolved.Initialize(tile);
        selectedCreature = evolved;

        deathPoints--;
        SpendAction(1);
        UpdateDeathPointsUI();
        RefreshHighlights();
        Debug.Log("Evolved into: " + evolved.name);
    }

    // ────────────────────────────────────────────────────────────────────────

    public void ClearSelectionState()
    {
        selectedCreature = null;
        BlackBoard.gridManager.ResetGridHighlights();
        if (Tile.selectedTile != null)
        {
            Tile.selectedTile.SetMaterial(Tile.selectedTile.originalMaterial);
            Tile.selectedTile = null;
        }
    }

    public void CheckVictory()
    {
        Creature[] allCreatures = FindObjectsByType<Creature>(FindObjectsSortMode.None);
        Debug.Log("CheckVictory called. Creatures found: " + allCreatures.Length);

        for (int p = 0; p < amountOfPlayers; p++)
        {
            bool hasAny = false;
            bool allDead = true;

            foreach (Creature c in allCreatures)
            {
                if (!c.enabled) continue;

                if (c.assignedPlayer == p)
                {
                    hasAny = true;
                    if (c.health > 0)
                    {
                        allDead = false;
                        break;
                    }
                }
            }

            if (hasAny && allDead)
            {
                int winner = (p == 0) ? 1 : 0;
                CreaturePreviewManager.Instance.HidePreview();
                CreatureUI.HideCurrentStats();
                LevelManager.Instance.OnBattleVictory(winner);
                return;
            }
        }
    }
}