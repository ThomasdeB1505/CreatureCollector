using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Shockwave")]
public class ShockwaveMove : CreatureMove
{
    public int damage = 15;
    public int radius = 1;

    void Reset()
    {
        moveName = "Shockwave";
        description = "Damage a target and everything around it.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        List<Tile> inRange = BlackBoard.gridManager.GetTilesInRange(user.currentTile, user.attackMinRange, user.attackRange);
        foreach (Tile t in inRange)
        {
            if (t.currentCreatureOnTile != null && !t.currentCreatureOnTile.dead)
                result.Add(t);
        }
        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        if (targetTile == null) return;

        // Hit the target itself
        if (targetTile.currentCreatureOnTile != null && !targetTile.currentCreatureOnTile.dead)
            targetTile.currentCreatureOnTile.TakeDamage(damage);

        // Then the ring around it
        List<Tile> surrounding = BlackBoard.gridManager.GetTilesInRange(targetTile, 0, radius);
        foreach (Tile t in surrounding)
        {
            if (t == targetTile) continue;
            if (t.currentCreatureOnTile != null && !t.currentCreatureOnTile.dead)
                t.currentCreatureOnTile.TakeDamage(damage);
            if (t.currentObstacle != null)
                t.currentObstacle.TakeDamage(damage);
        }
    }
}