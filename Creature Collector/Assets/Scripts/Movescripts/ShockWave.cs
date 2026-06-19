using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Shockwave")]
public class Shockwave : CreatureMove
{
    public int damage = 15;
    public int radius = 1;

    public override void Execute(Creature user, Creature target)
    {
        List<Tile> tiles = BlackBoard.gridManager.GetTilesInRange(target.currentTile, radius);
        foreach (Tile t in tiles)
        {
            if (t.currentCreatureOnTile != null && !t.currentCreatureOnTile.dead)
                t.currentCreatureOnTile.TakeDamage(damage);
        }
    }
}