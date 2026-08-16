using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Shockwave")]
public class ShockwaveMove : CreatureMove
{
    public int damage = 15;
    public int radius = 1;

    public override List<Tile> GetAffectedTiles(Tile centerTile)
    {
        List<Tile> result = new List<Tile>();
        if (centerTile == null) return result;
        List<Tile> surrounding = BlackBoard.gridManager.GetTilesInRange(centerTile, 0, radius);
        foreach (Tile t in surrounding)
            result.Add(t);
        if (!result.Contains(centerTile))
            result.Add(centerTile);
        return result;
    }
    void Reset()
    {
        moveName = "Shockwave";
        description = "Damage a target and everything around it.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        return BlackBoard.gridManager.GetTilesInRange(user.currentTile, user.attackMinRange, user.attackRange);
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        Tile centerTile = targetTile != null ? targetTile : user.currentTile;
        if (centerTile == null) return;

        if (centerTile.currentCreatureOnTile != null && !centerTile.currentCreatureOnTile.dead)
            centerTile.currentCreatureOnTile.TakeDamage(damage);

        List<Tile> surrounding = BlackBoard.gridManager.GetTilesInRange(centerTile, 0, radius);
        foreach (Tile t in surrounding)
        {
            if (t == centerTile) continue;
            if (t.currentCreatureOnTile != null && !t.currentCreatureOnTile.dead)
                t.currentCreatureOnTile.TakeDamage(damage);
            if (t.currentObstacle != null)
                t.currentObstacle.TakeDamage(damage);
        }
    }
}