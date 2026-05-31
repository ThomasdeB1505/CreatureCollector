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

    private void Awake()
    {
        BlackBoard.gameManager = this;
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
    }

    void EndTurn()
    {
        currentPlayer++;
        if(currentPlayer >= amountOfPlayers )
            currentPlayer = 0;
        StartTurn();
    }

    void SpendAction(int actionCost)
    {
        currentTurnActions -= actionCost;
        if (currentTurnActions <= 0)
            EndTurn();
    }

    public void ClickOnTile(Tile _clicked)
    {
        if(selectedCreature != null)
        {
            Vector2Int creatureTilePos = selectedCreature.currentTile.gridPosition;
            Vector2Int clickedTilePos = _clicked.gridPosition;

            if (_clicked.currentCreatureOnTile != null)
            {
                if(_clicked.currentCreatureOnTile == selectedCreature || _clicked.currentCreatureOnTile.dead)
                {
                    Deselect();
                    return;
                }

                else
                {
                    //attack?
                    if ((creatureTilePos.x != clickedTilePos.x && creatureTilePos.y != clickedTilePos.y)
                        || selectedCreature.currentTile == _clicked)
                    {
                        //not a straight line (or clicked on the same tile)
                        Deselect();
                        return;
                    }

                    if (creatureTilePos.x != clickedTilePos.x)
                    {
                        //move in x direction

                        //check if the creature can move the distance
                        if (Mathf.Sqrt(Mathf.Pow(creatureTilePos.x - clickedTilePos.x, 2)) > selectedCreature.attackRange)
                        {
                            Deselect();
                            return;
                        }

                        //it can move there! So move it there!!
                        selectedCreature.Attack(_clicked.currentCreatureOnTile);
                        SpendAction(selectedCreature.attackActionCost);
                        Deselect();
                    }
                    else if (creatureTilePos.y != clickedTilePos.y)
                    {
                        //move in y direction
                        if (Mathf.Sqrt(Mathf.Pow(creatureTilePos.y - clickedTilePos.y, 2)) > selectedCreature.attackRange)
                        {
                            Deselect();
                            return;
                        }

                        selectedCreature.Attack(_clicked.currentCreatureOnTile);
                        SpendAction(selectedCreature.attackActionCost);
                        Deselect();
                    }

                }
            }

            else
            {
                //check if we can move there
                //for now we only move in straight lines, it's easier
                if ((creatureTilePos.x != clickedTilePos.x && creatureTilePos.y != clickedTilePos.y)
                    || selectedCreature.currentTile == _clicked)
                {
                    //not a straight line (or clicked on the same tile)
                    Deselect();
                    return;
                }

                if(creatureTilePos.x != clickedTilePos.x)
                {
                    //move in x direction

                    //check if the creature can move the distance
                    int distanceBetweenTiles = (int)Mathf.Sqrt(Mathf.Pow(creatureTilePos.x - clickedTilePos.x, 2));

                    if (distanceBetweenTiles > selectedCreature.moveRange)
                    {
                        Deselect();
                        return;
                    }

                    //TODO: check if there is something in the way (dead creature??)
                    if(distanceBetweenTiles > 1)
                    {
                        int direction = 1;
                        
                        //move the other way
                        if (creatureTilePos.x > clickedTilePos.x)
                            direction = -1;

                        for(int i  = 1; i < distanceBetweenTiles - 1; i++)
                        {
                            if(BlackBoard.gridManager.map[selectedCreature.currentTile.gridPosition.x + direction * i, selectedCreature.currentTile.gridPosition.y].currentCreatureOnTile != null)
                            {
                                Deselect();
                                return;
                            }
                        }
                    }

                    //it can move there! So move it there!!
                    selectedCreature.Moveto(_clicked.transform.position, _clicked);
                    SpendAction(selectedCreature.moveActionCost);
                    Deselect();
                }
                else if (creatureTilePos.y != clickedTilePos.y)
                {
                    int distanceBetweenTiles = (int)Mathf.Sqrt(Mathf.Pow(creatureTilePos.y - clickedTilePos.y, 2));

                    //move in y direction
                    if (distanceBetweenTiles > selectedCreature.moveRange)
                    {
                        Deselect();
                        return;
                    }


                    //TODO: check if there is something in the way (dead creature??)
                    if (distanceBetweenTiles > 1)
                    {
                        int direction = 1;

                        //move the other way
                        if (creatureTilePos.y > clickedTilePos.y)
                            direction = -1;

                        for (int i = 1; i < distanceBetweenTiles - 1; i++)
                        {
                            if (BlackBoard.gridManager.map[selectedCreature.currentTile.gridPosition.x, selectedCreature.currentTile.gridPosition.y + direction * i].currentCreatureOnTile != null)
                            {
                                Deselect();
                                return;
                            }
                        }
                    }

                    selectedCreature.Moveto(_clicked.transform.position, _clicked);
                    SpendAction(selectedCreature.moveActionCost);
                    Deselect();
                }

            }
        }
        else
        {
            if (_clicked.currentCreatureOnTile != null)
            {
                if (!_clicked.currentCreatureOnTile.dead)
                {
                    if (_clicked.currentCreatureOnTile.assignedPlayer == currentPlayer)
                    {
                        selectedCreature = _clicked.currentCreatureOnTile;
                        return;
                    }
                }
            }
            Deselect();
        }
    }

    void Deselect()
    {
        selectedCreature = null;
        Tile.selectedTile.ChangeColor(Tile.selectedTile.originalColor);
        Tile.selectedTile = null;
    }


}
