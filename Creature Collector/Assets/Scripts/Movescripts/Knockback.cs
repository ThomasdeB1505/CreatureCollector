using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Knockback")]
public class KnockbackMove : CreatureMove
{
    public int damage = 10;

    void Reset()
    {
        moveName = "Knockback";
        description = "Damage an enemy and push it back one tile.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        List<Tile> inRange = BlackBoard.gridManager.GetTilesInRange(user.currentTile, user.attackMinRange, user.attackRange);
        foreach (Tile t in inRange)
        {
            if (t.currentCreatureOnTile != null && !t.currentCreatureOnTile.dead && t.currentCreatureOnTile.assignedPlayer != user.assignedPlayer)
                result.Add(t);
        }
        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        if (targetCreature == null) return;

        targetCreature.TakeDamage(damage);

        Vector2Int dir = targetCreature.gridPosition - user.gridPosition;
        dir = new Vector2Int((int)Mathf.Sign(dir.x), (int)Mathf.Sign(dir.y));
        Vector2Int pushPos = targetCreature.gridPosition + dir;

        Tile pushTile = BlackBoard.gridManager.GetTileAt(pushPos);
        if (pushTile != null && pushTile.currentCreatureOnTile == null && pushTile.currentObstacle == null && !pushTile.blocked)
            targetCreature.Moveto(pushTile.transform.position, pushTile);
    }
}