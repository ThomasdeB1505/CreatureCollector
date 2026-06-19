using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Selfdestruct")]
public class Selfdestruct : CreatureMove
{
    public int damage = 40;
    public int radius = 1;

    public override void Execute(Creature user, Creature target)
    {
        List<Tile> tiles = BlackBoard.gridManager.GetTilesInRange(user.currentTile, radius);
        foreach (Tile t in tiles)
        {
            if (t.currentCreatureOnTile != null && t.currentCreatureOnTile != user && !t.currentCreatureOnTile.dead)
                t.currentCreatureOnTile.TakeDamage(damage);
        }
        user.Die();
    }
}
