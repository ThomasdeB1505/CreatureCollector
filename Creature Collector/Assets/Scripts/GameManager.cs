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

    private void Awake()
    {
        BlackBoard.gameManager = this;
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

        UpdateTurnUI();
    }

    void EndTurn()
    {
        currentPlayer++;
        if(currentPlayer >= amountOfPlayers )
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
        if (currentTurnActions <= 0)
            EndTurn();
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
                // clicking a friendly creature selects it instead
                if (_clicked.currentCreatureOnTile.assignedPlayer == currentPlayer)
                {
                    selectedCreature = _clicked.currentCreatureOnTile;
                    RefreshHighlights(); // replaces the two HighlightMoveRange/AttackRange calls
                    return;
                }

                // Attack
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
                // Move
                List<Tile> moveRange = BlackBoard.gridManager.GetTilesInRange(
                    selectedCreature.currentTile, selectedCreature.moveRange);

                if (!moveRange.Contains(_clicked))
                {
                    Deselect();
                    return;
                }

                // TODO: obstacle checking along path
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
                RefreshHighlights(); // replaces the two HighlightMoveRange/AttackRange calls
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


}
