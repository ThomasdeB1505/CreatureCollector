using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Selfdestruct")]
public class Selfdestruct : CreatureMove
{
    public int damage = 40;
    public int radius = 1;

    void Reset()
    {
        moveName = "Selfdestruct";
        description = "Explode, damaging everything nearby, and die.";
        requiresTarget = false;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        List<Tile> tiles = BlackBoard.gridManager.GetTilesInRange(user.currentTile, 1, radius);
        foreach (Tile t in tiles)
        {
            if (t.currentCreatureOnTile != null && t.currentCreatureOnTile != user && !t.currentCreatureOnTile.dead)
                t.currentCreatureOnTile.TakeDamage(damage);
        }
        user.Die();

        BlackBoard.gameManager.CheckVictory();
    }
}