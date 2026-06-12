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
    public TextMeshProUGUI deathPointsText;  // UI text showing current death points
    public Button evolveButton;              // button that triggers TryEvolveSelected
    private bool battleStarted = false;
    private bool isCapturing = false;
    public void SetCapturing(bool value) => isCapturing = value;
    public Material[] playerMaterials; // index 0 = blue (player 1), index 1 = red (player 2)

    private void Awake()
    {
        BlackBoard.gameManager = this;
    }

    public Creature GetSelectedCreature()
    {
        return selectedCreature;
    }

    //doing this so turn UI is displayed correctly
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
        actionPointsUI.SetupCircles(actionsPerTurn); // ADD
        actionPointsUI.UpdateCircles(currentTurnActions, actionsPerTurn); // ADD
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

        if (currentPlayer == 1) // Player 2 is AI
            StartCoroutine(SimpleAI.Instance.TakeTurn(actionsPerTurn));
    }
    void UpdateTurnUI()
    {
        turnText.text = "Player " + (currentPlayer + 1) + " Turn";
    }

    void SpendAction(int actionCost)
    {
        currentTurnActions -= actionCost;
        actionPointsUI.UpdateCircles(currentTurnActions, actionsPerTurn); // ADD
    }
    bool HasActionsLeft()
    {
        return currentTurnActions > 0;
    }

    public void ClickOnTile(Tile _clicked)
    {
        if (isCapturing) return; // block all tile input during capture
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

                if (!HasActionsLeft()) { Deselect(); return; } // ADD

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
                if (!HasActionsLeft()) { Deselect(); return; } // ADD

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
        UpdateDeathPointsUI(); // ADD
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
            deathPointsText.text = "Death Points: " + deathPoints;
        // Also update evolve button interactability
        if (evolveButton != null)
            evolveButton.interactable = deathPoints > 0 && selectedCreature != null
                && selectedCreature.evolvedFormPrefab != null
                && !selectedCreature.isEvolved;
    }

    public void TryEvolveSelected()
    {
        if (selectedCreature == null) return;
        if (selectedCreature.evolvedFormPrefab == null) return;
        if (selectedCreature.isEvolved) return;
        if (deathPoints <= 0) return;
        if (!HasActionsLeft()) return;

        Tile tile = selectedCreature.currentTile;
        int player = selectedCreature.assignedPlayer;

        GameObject oldObj = selectedCreature.gameObject;

        Creature evolved = Instantiate(selectedCreature.evolvedFormPrefab)
            .GetComponent<Creature>();

        evolved.assignedPlayer = player;
        evolved.isEvolved = true;

        // IMPORTANT: break old tile reference BEFORE destroy
        tile.currentCreatureOnTile = null;

        Destroy(oldObj);

        evolved.Initialize(tile);

        selectedCreature = evolved;

        deathPoints--;
        SpendAction(1);
        UpdateDeathPointsUI();
        RefreshHighlights();
        Debug.Log("Evolving: " + selectedCreature.name);
    }
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
