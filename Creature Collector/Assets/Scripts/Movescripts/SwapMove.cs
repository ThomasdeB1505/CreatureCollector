using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Swap")]
public class SwapMove : CreatureMove
{
    void Reset()
    {
        moveName = "Swap";
        description = "Swap places with any friendly creature on the field.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        Creature[] all = Object.FindObjectsByType<Creature>(FindObjectsSortMode.None);

        foreach (Creature c in all)
        {
            if (c != user && c.assignedPlayer == user.assignedPlayer && !c.dead)
                result.Add(c.currentTile);
        }
        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        if (targetCreature == null) return;

        Tile userTile = user.currentTile;
        Tile otherTile = targetCreature.currentTile;

        userTile.currentCreatureOnTile = targetCreature;
        otherTile.currentCreatureOnTile = user;

        user.gridPosition = otherTile.gridPosition;
        user.currentTile = otherTile;
        user.transform.position = otherTile.transform.position;

        targetCreature.gridPosition = userTile.gridPosition;
        targetCreature.currentTile = userTile;
        targetCreature.transform.position = userTile.transform.position;
    }
}