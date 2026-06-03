using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    }

    void Deselect()
    {
        selectedCreature = null;
        BlackBoard.gridManager.ResetGridHighlights(); // ADD THIS
        Tile.selectedTile.SetMaterial(Tile.selectedTile.originalMaterial);
        Tile.selectedTile = null;
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
                victoryText.text = "Player " + (winner + 1) + " Wins!";
                victoryScreen.SetActive(true);
                return;
            }
        }
    }

}
